using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Application;
using Domain;
using System.Security.Claims;
using System;

namespace Api.Endpoints;

public static class CitasEndpoints
{
    public static void MapCitasEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/citas");

        group.MapPost("/", async (
            CrearCitaRequestDto req, 
            CitaService service, 
            ClaimsPrincipal user) =>
        {
            try
            {
                // Extraer el ID del usuario actual desde el Token JWT
                var idUsuarioClaim = user.FindFirst("idUsuario")?.Value;
                if (!int.TryParse(idUsuarioClaim, out int idUsuarioToken))
                    return Results.Unauthorized();

                var idCita = await service.AgendarCitaAsync(req, idUsuarioToken);
                return Results.Ok(new { message = "Cita agendada con éxito", idCita });
            }
            catch (InvalidOperationException ex) // Aquí atrapamos el error de concurrencia 23505
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex) // Errores de validación de negocio
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem("Ocurrió un error inesperado al procesar la cita.");
            }
        }).RequireAuthorization(); // Requiere estar logueado
    }
}