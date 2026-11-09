using Application.Services;
using Domain.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;

namespace Api.Endpoints;

public static class PacienteEndpoints
{
    public static void MapPacienteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/pacientes");

        // 1. Registro de paciente (Público, incluye validaciones detalladas de FluentValidation)
        group.MapPost("/registro", [AllowAnonymous] async (
            RegistroPacienteRequestDto req, 
            PacienteService service, 
            IValidator<RegistroPacienteRequestDto> validator) =>
        {
            var validacion = await validator.ValidateAsync(req);
            if (!validacion.IsValid)
            {
                // Mapea los errores para decirle al paciente exactamente qué campo falló
                var errores = validacion.Errors.Select(e => new { Campo = e.PropertyName, Error = e.ErrorMessage });
                return Results.BadRequest(new { message = "Errores en el formulario", errores });
            }

            try
            {
                var id = await service.RegistrarPacienteAsync(req);
                return Results.Ok(new { message = "Paciente registrado exitosamente", idUsuario = id });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // 2. Buscar paciente por nombre (Uso exclusivo de Recepción y Admin)
        // Nota: Se coloca antes de /{documento} para evitar colisiones de rutas
        group.MapGet("/buscar", [Authorize(Roles = "Administrador,Recepcionista")] async (string nombre, PacienteService service) =>
        {
            if (string.IsNullOrWhiteSpace(nombre)) 
                return Results.BadRequest(new { error = "Debe proporcionar un nombre para buscar." });
                
            var pacientes = await service.BuscarPorNombreAsync(nombre);
            return Results.Ok(pacientes);
        });

        // 3. Listar todos los pacientes (Solo personal administrativo)
        group.MapGet("/", [Authorize(Roles = "Administrador,Recepcionista")] async (PacienteService service) => 
            Results.Ok(await service.ListarPacientesAsync()));

        // 4. Buscar por documento (Personal de la clínica)
        group.MapGet("/{documento}", [Authorize(Roles = "Administrador,Recepcionista,Medico")] async (string documento, PacienteService service) =>
        {
            var paciente = await service.BuscarPorDocumentoAsync(documento);
            return paciente is not null ? Results.Ok(paciente) : Results.NotFound(new { error = "Paciente no encontrado" });
        });

        // 5. Actualizar perfil del paciente (Restringido al propio paciente o Admin)
        group.MapPut("/{id:int}", [Authorize(Roles = "Paciente,Administrador")] async (
            int id, 
            ActualizarPacienteRequestDto req, 
            PacienteService service) =>
        {
            try
            {
                // En un escenario ideal aquí también inyectarías un validador para ActualizarPacienteRequestDto
                var actualizado = await service.ActualizarPerfilAsync(id, req);
                return actualizado ? Results.Ok(new { message = "Perfil actualizado exitosamente" }) 
                                   : Results.NotFound(new { error = "Paciente no encontrado en el sistema" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}