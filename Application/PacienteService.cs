using Domain;
using Infrastructure;
using BCrypt.Net;

namespace Application;

public class PacienteService
{
    private readonly PacienteRepository _repo;
    
    public PacienteService(PacienteRepository repo) => _repo = repo;

    public async Task<int> RegistrarPacienteAsync(RegistroPacienteRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Contrasena))
            throw new ArgumentException("El correo y la contraseña son obligatorios");

        string hash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena);
        return await _repo.CrearPacienteTransaccionalAsync(dto, hash);
    }

    public Task<IEnumerable<PacienteResponseDto>> ListarPacientesAsync() => _repo.ObtenerTodosAsync();
    
    public Task<PacienteResponseDto?> BuscarPorDocumentoAsync(string doc) => _repo.ObtenerPorDocumentoAsync(doc);
}