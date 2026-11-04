using Application;
using Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/auth/login", async (LoginRequestDto req, AuthService service) =>
        {
            var result = await service.AutenticarAsync(req);
            return result is not null 
                ? Results.Ok(result) 
                : Results.Unauthorized();
        });
    }
}