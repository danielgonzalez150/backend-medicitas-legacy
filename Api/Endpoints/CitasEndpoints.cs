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
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                int? idPacienteToken = null;
                
                if (!string.IsNullOrEmpty(idPacienteClaim) && int.TryParse(idPacienteClaim, out int parsedId))
                {
                    idPacienteToken = parsedId;
                }

                // Determinar qué ID de paciente usar: el del request (si lo envía un recepcionista/admin) o el del token
                var idPacienteFinal = req.IdPaciente ?? idPacienteToken;

                if (idPacienteFinal == null || idPacienteFinal <= 0)
                {
                    return Results.BadRequest(new { error = "No se pudo identificar un perfil de paciente válido. Envíe el IdPaciente en la petición o inicie sesión con una cuenta de paciente válida." });
                }

                var idCita = await service.AgendarCitaAsync(req, idPacienteFinal.Value);
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
                Console.WriteLine($"[ERROR AGENDAR CITA]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });

        // 2. GET /api/medicos/{idMedico}/disponibilidad
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
                Console.WriteLine($"[ERROR DISPONIBILIDAD]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });

        // 3. GET /api/citas/paciente
        citasGroup.MapGet("/paciente", async (
            DateOnly fechaInicio, 
            DateOnly fechaFin, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                
                if (string.IsNullOrEmpty(idPacienteClaim) || !int.TryParse(idPacienteClaim, out int idPacienteToken))
                {
                    return Results.BadRequest(new { error = "Tu usuario actual no tiene un perfil de paciente asociado para consultar citas." });
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
                Console.WriteLine($"[ERROR CONSULTA PACIENTE]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });

        // 4. GET /api/citas/medico/{idMedico}
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
                Console.WriteLine($"[ERROR CONSULTA MEDICO]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });

        // 5. PUT /api/citas/{id}/cancelar
        citasGroup.MapPut("/{id:int}/cancelar", async (
            int id, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                var idPacienteClaim = user.FindFirst("IdPaciente")?.Value;
                int.TryParse(idPacienteClaim, out int idPacienteToken); 

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
                Console.WriteLine($"[ERROR CANCELAR CITA]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });

        // 6. PUT /api/citas/{id}/reprogramar
        citasGroup.MapPut("/{id:int}/reprogramar", async (
            int id, 
            ReprogramarCitaRequestDto req, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
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
                Console.WriteLine($"[ERROR REPROGRAMAR CITA]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });

        // 7. PUT /api/citas/{id}/estado
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
                Console.WriteLine($"[ERROR ESTADO CITA]: {ex}");
                return Results.Problem(
                    detail: $"{ex.Message} | Inner: {ex.InnerException?.Message}",
                    statusCode: 500
                );
            }
        });
    }
}