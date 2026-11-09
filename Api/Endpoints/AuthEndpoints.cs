using Application.Services;
using Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/auth/login", [AllowAnonymous] async (LoginRequestDto req, AuthService service) =>
        {
            try
            {
                var result = await service.AutenticarAsync(req);
                return result is not null 
                    ? Results.Ok(result) 
                    : Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Error al procesar la autenticación", detalle = ex.Message });
            }
        });
    }
}