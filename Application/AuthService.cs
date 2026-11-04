using Domain;
using Infrastructure;
using BCrypt.Net;

namespace Application;

public class AuthService
{
    private readonly UsuarioRepository _repo;
    
    public AuthService(UsuarioRepository repo) => _repo = repo;

    public async Task<AuthResponseDto?> AutenticarAsync(LoginRequestDto request)
    {
        var user = await _repo.ObteneporCorreoAsync(request.Correo);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Contrasena, user.ContrasenaHash))
            return null;

        return new AuthResponseDto(user.IdUsuario, user.Correo, user.IdRol);
    }
}