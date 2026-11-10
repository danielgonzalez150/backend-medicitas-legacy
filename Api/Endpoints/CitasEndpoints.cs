using Application.Services;
using Domain.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Api.Endpoints;

public static class CitasEndpoints
{
    public static void MapCitasEndpoints(this IEndpointRouteBuilder routes)
    {
        var citasGroup = routes.MapGroup("/api/citas").RequireAuthorization();
        var medicosGroup = routes.MapGroup("/api/medicos").RequireAuthorization();

        // 1. POST /api/citas -> Agendar cita
        citasGroup.MapPost("/", async (
            CrearCitaRequestDto req, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                var idUsuarioClaim = user.FindFirst("idUsuario")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idUsuarioClaim, out int idUsuarioToken))
                    return Results.Unauthorized();

                var idCita = await service.AgendarCitaAsync(req, idUsuarioToken);
                return Results.Ok(new { message = "Cita agendada con éxito", idCita });
            }
            catch (InvalidOperationException ex) // Atrapa violaciones de índice único 23505 (Concurrencia)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex) // Errores de fecha en el pasado
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al procesar la cita.");
            }
        });

        // 2. GET /api/medicos/{idMedico}/disponibilidad?fecha=YYYY-MM-DD -> Franjas horarias libres
        medicosGroup.MapGet("/{idMedico:int}/disponibilidad", async (
            int idMedico, 
            DateOnly fechaInicio, 
            DateOnly fechaFin, 
            CitaService service) =>
        {
            try
            {
                var disponibilidad = await service.ObtenerFranjasDisponiblesAsync(idMedico, fechaInicio, fechaFin);
                return Results.Ok(disponibilidad);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem("Ocurrió un error inesperado al consultar la disponibilidad.");
            }
        });

        // 3. GET /api/citas/paciente?fechaInicio=YYYY-MM-DD&fechaFin=YYYY-MM-DD -> Mis Citas (Paciente)
        citasGroup.MapGet("/paciente", async (
            DateOnly fechaInicio, 
            DateOnly fechaFin, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                var idUsuarioClaim = user.FindFirst("idUsuario")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idUsuarioClaim, out int idPacienteToken))
                    return Results.Unauthorized();

                var citas = await service.ObtenerAgendaPacienteAsync(idPacienteToken, fechaInicio, fechaFin);
                return Results.Ok(citas);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al consultar las citas del paciente.");
            }
        });

        // 4. GET /api/citas/medico/{idMedico}?fechaInicio=YYYY-MM-DD&fechaFin=YYYY-MM-DD -> Agenda del Médico
        citasGroup.MapGet("/medico/{idMedico:int}", async (
            int idMedico, 
            DateOnly fechaInicio, 
            DateOnly fechaFin, 
            CitaService service) =>
        {
            try
            {
                var citas = await service.ObtenerAgendaMedicoAsync(idMedico, fechaInicio, fechaFin);
                return Results.Ok(citas);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al consultar la agenda del médico.");
            }
        });

        // 5. PUT /api/citas/{id}/cancelar -> Cancelar cita
        citasGroup.MapPut("/{id:int}/cancelar", async (
            int id, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                var idUsuarioClaim = user.FindFirst("idUsuario")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idUsuarioClaim, out int idUsuarioToken))
                    return Results.Unauthorized();

                var rolUsuario = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("rol")?.Value ?? "";

                var cancelado = await service.CancelarCitaAsync(id, idUsuarioToken, rolUsuario);
                if (cancelado)
                    return Results.Ok(new { message = "Cita cancelada con éxito." });

                return Results.BadRequest(new { error = "No se pudo cancelar la cita." });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al cancelar la cita.");
            }
        });

        // 6. PUT /api/citas/{id}/reprogramar -> Reprogramar fecha/hora
        citasGroup.MapPut("/{id:int}/reprogramar", async (
            int id, 
            ReprogramarCitaRequestDto req, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                var idUsuarioClaim = user.FindFirst("idUsuario")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(idUsuarioClaim, out int idUsuarioToken))
                    return Results.Unauthorized();

                var rolUsuario = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("rol")?.Value ?? "";

                var reprogramado = await service.ReprogramarCitaAsync(id, req, idUsuarioToken, rolUsuario);
                if (reprogramado)
                    return Results.Ok(new { message = "Cita reprogramada con éxito." });

                return Results.BadRequest(new { error = "No se pudo reprogramar la cita." });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex) // Cruce de franja horaria en reprogramación
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al reprogramar la cita.");
            }
        });

        // 7. PUT /api/citas/{id}/estado -> Cambiar estado (Atendida/Inasistencia)
        citasGroup.MapPut("/{id:int}/estado", async (
            int id, 
            CambiarEstadoCitaDto dto, 
            CitaService service) =>
        {
            try
            {
                var actualizado = await service.CambiarEstadoCitaAsync(id, dto.IdEstado);
                if (actualizado)
                    return Results.Ok(new { message = "Estado de la cita actualizado con éxito." });

                return Results.BadRequest(new { error = "No se pudo actualizar el estado de la cita." });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al actualizar el estado de la cita.");
            }
        });
    }
}