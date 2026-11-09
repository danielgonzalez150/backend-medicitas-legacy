using Application.Services;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace Api.Endpoints;

public static class HorarioEndpoints
{
    public static void MapHorarioEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/medicos/{idMedico:int}");

        // 1. Configurar u hora de trabajo para un médico (Administrador o el propio Médico)
        group.MapPost("/horarios", [Authorize(Roles = "Administrador,Medico")] async (
            int idMedico, 
            ConfigurarHorarioRequestDto req, 
            HorarioService service) =>
        {
            try
            {
                var idHorario = await service.ConfigurarHorarioAsync(idMedico, req);
                return Results.Ok(new { message = "Horario configurado exitosamente", idHorario });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Ocurrió un error al guardar el horario.", detalle = ex.Message });
            }
        });

        // 2. Obtener la lista de horarios configurados de un médico (Administrador, Médico o Recepcionista)
        group.MapGet("/horarios", [Authorize(Roles = "Administrador,Medico,Recepcionista")] async (
            int idMedico, 
            HorarioService service) =>
        {
            var horarios = await service.ObtenerHorariosPorMedicoAsync(idMedico);
            return Results.Ok(horarios);
        });

        // 3. Obtener las franjas horarias disponibles para agendar cita en una fecha (Accesible por Pacientes y Recepción)
        group.MapGet("/disponibilidad", [Authorize(Roles = "Paciente,Recepcionista,Administrador")] async (
            int idMedico, 
            DateOnly fecha, 
            HorarioService service) =>
        {
            try
            {
                var franjas = await service.ObtenerFranjasDisponiblesAsync(idMedico, fecha);
                return Results.Ok(franjas);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Error al consultar disponibilidad.", detalle = ex.Message });
            }
        });
    }
}