using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.DTOs;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services;

public class AuthService
{
    private readonly UsuarioRepository _repo;
    private readonly IConfiguration _config;
    
    // Se inyecta IConfiguration para acceder a las variables de entorno o appsettings.json
    public AuthService(UsuarioRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    public async Task<AuthResponseDto?> AutenticarAsync(LoginRequestDto request)
    {
        var user = await _repo.ObteneporCorreoAsync(request.Correo);
        
        // Se valida que el usuario exista, que la contraseña sea correcta y que su estado sea true (activo)
        if (user == null || !user.Estado || !BCrypt.Net.BCrypt.Verify(request.Contrasena, user.ContrasenaHash))
            return null;

        var token = GenerarJwtToken(user);

        // Retornamos el DTO incluyendo el nuevo token generado
        return new AuthResponseDto(user.IdUsuario, user.Correo, user.IdRol, token);
    }

    private string GenerarJwtToken(UsuarioDbDto user) 
    {
        // Mapeamos el ID del rol al nombre según tu script SQL para que el middleware de .NET lo entienda
        string rolNombre = user.IdRol switch
        {
            1 => "Administrador",
            2 => "Medico",
            3 => "Paciente",
            _ => "Usuario"
        };

        // Obtenemos la llave desde appsettings.json (o usamos una de respaldo para desarrollo)
        var key = _config["Jwt:Key"] ?? "ClaveSuperSecretaDeDesarrolloMediCitas12345!";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Se usa una lista dinámica para poder agregar claims opcionales
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
            new Claim(ClaimTypes.Email, user.Correo),
            new Claim(ClaimTypes.Role, rolNombre) // Este Claim es el que bloquea o permite el acceso a las rutas
        };

        // Inyectamos el IdPaciente si el usuario tiene uno asociado
        if (user.IdPaciente.HasValue)
        {
            claims.Add(new Claim("IdPaciente", user.IdPaciente.Value.ToString()));
        }

        // Inyectamos el IdMedico si el usuario tiene uno asociado
        if (user.IdMedico.HasValue)
        {
            claims.Add(new Claim("IdMedico", user.IdMedico.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(8), // El token expira en 8 horas
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}