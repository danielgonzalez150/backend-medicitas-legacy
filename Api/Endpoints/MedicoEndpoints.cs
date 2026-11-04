using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class MedicoEndpoints
{
    public static void MapMedicoEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/medicos", async (MedicoRepository repo) => 
            Results.Ok(await repo.ObtenerTodosAsync()));
    }
}