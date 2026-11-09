using Application.Services;
using Domain.DTOs;
using FluentValidation;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;

namespace Api.Endpoints;

public static class MedicoEndpoints
{
    public static void MapMedicoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/medicos");

        // 1. Obtener directorio de médicos (Público o accesible por cualquier usuario)
        group.MapGet("/", async (MedicoRepository repo) => 
            Results.Ok(await repo.ObtenerTodosAsync()));

        // 2. Registrar un nuevo médico (Exclusivo para el Administrador)
        group.MapPost("/", [Authorize(Roles = "Administrador")] async (
            CrearMedicoRequestDto req, 
            MedicoService service, 
            IValidator<CrearMedicoRequestDto> validator) =>
        {
            var validacion = await validator.ValidateAsync(req);
            if (!validacion.IsValid)
            {
                var errores = validacion.Errors.Select(e => new { Campo = e.PropertyName, Error = e.ErrorMessage });
                return Results.BadRequest(new { message = "Errores en el formulario", errores });
            }

            try
            {
                var id = await service.RegistrarMedicoAsync(req);
                return Results.Ok(new { message = "Médico registrado exitosamente", idMedico = id });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 3. Actualizar el estado del médico (Activar/Desactivar - Exclusivo para el Administrador)
        group.MapPut("/{id:int}/estado", [Authorize(Roles = "Administrador")] async (
            int id, 
            DesactivarMedicoRequestDto req, 
            MedicoService service) =>
        {
            try
            {
                var actualizado = await service.DesactivarMedicoAsync(id, req);
                return actualizado 
                    ? Results.Ok(new { message = "Estado del médico actualizado exitosamente" }) 
                    : Results.NotFound(new { error = "Médico no encontrado en el sistema" });
            }
            catch (InvalidOperationException ex) 
            {
                // Regla de negocio del Sprint 1: Error 409 Conflict si el médico tiene citas pendientes
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}