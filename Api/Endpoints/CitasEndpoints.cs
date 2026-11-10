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
                // CORRECCIÓN: Usar explícitamente el claim IdPaciente
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                int idPacienteToken = 0;
                
                if (!string.IsNullOrEmpty(idPacienteClaim))
                {
                    int.TryParse(idPacienteClaim, out idPacienteToken);
                }

                // Si no hay IdPaciente en el token y tampoco en el request, no se puede agendar
                if (idPacienteToken == 0 && req.IdPaciente == null)
                {
                    return Results.BadRequest(new { error = "No se pudo identificar al paciente. Envíe el IdPaciente en la petición o inicie sesión como paciente." });
                }

                var idCita = await service.AgendarCitaAsync(req, idPacienteToken);
                return Results.Ok(new { message = "Cita agendada con éxito", idCita });
            }
            catch (InvalidOperationException ex) 
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex) 
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al procesar la cita.");
            }
        });

        // 2. GET /api/medicos/{idMedico}/disponibilidad?fechaInicio=YYYY-MM-DD&fechaFin=YYYY-MM-DD -> Franjas horarias libres
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
            catch (Exception ex)
            {
                // CORRECCIÓN: Mostrar el error real de base de datos/backend para diagnosticar el 500
                return Results.Problem($"Error interno: {ex.Message} | StackTrace: {ex.StackTrace}");
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
                // CORRECCIÓN: Extraer explícitamente el claim IdPaciente, no el NameIdentifier
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                
                if (string.IsNullOrEmpty(idPacienteClaim) || !int.TryParse(idPacienteClaim, out int idPacienteToken))
                {
                    return Results.Unauthorized(); 
                }

                var citas = await service.ObtenerAgendaPacienteAsync(idPacienteToken, fechaInicio, fechaFin);
                return Results.Ok(citas);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Ocurrió un error inesperado al consultar las citas del paciente: {ex.Message}");
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
                // CORRECCIÓN: Utilizar IdPaciente para la validación de propiedad de la cita
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                int.TryParse(idPacienteClaim, out int idPacienteToken); // Si es null, quedará en 0 (Admin/Recepcionista)

                var rolUsuario = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("rol")?.Value ?? "";

                var cancelado = await service.CancelarCitaAsync(id, idPacienteToken, rolUsuario);
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
                // CORRECCIÓN: Utilizar IdPaciente para la validación de propiedad de la cita
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                int.TryParse(idPacienteClaim, out int idPacienteToken); 

                var rolUsuario = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("rol")?.Value ?? "";

                var reprogramado = await service.ReprogramarCitaAsync(id, req, idPacienteToken, rolUsuario);
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
            catch (InvalidOperationException ex)
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