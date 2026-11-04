using Application;
using Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class PacienteEndpoints
{
    public static void MapPacienteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/pacientes");

        group.MapPost("/registro", async (RegistroPacienteRequestDto req, PacienteService service) =>
        {
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

        group.MapGet("/", async (PacienteService service) => 
            Results.Ok(await service.ListarPacientesAsync()));

        group.MapGet("/{documento}", async (string documento, PacienteService service) =>
        {
            var paciente = await service.BuscarPorDocumentoAsync(documento);
            return paciente is not null ? Results.Ok(paciente) : Results.NotFound(new { error = "Paciente no encontrado" });
        });
    }
}