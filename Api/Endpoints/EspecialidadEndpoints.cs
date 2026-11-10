using Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class EspecialidadEndpoints
{
    public static void MapEspecialidadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/especialidades").WithTags("Especialidades");

        group.MapGet("/", async (EspecialidadService especialidadService) =>
        {
            var especialidades = await especialidadService.ObtenerTodasAsync();
            
            if (especialidades == null)
            {
                return Results.NotFound("No se encontraron especialidades.");
            }

            return Results.Ok(especialidades);
        })
        .RequireAuthorization(); // Elimina esta línea si necesitas que el catálogo sea público (sin token JWT)
    }
}