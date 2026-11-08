using Domain;
using Infrastructure;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    // NUEVO: Método para actualizar el perfil del paciente
    public async Task<bool> ActualizarPerfilAsync(int idPaciente, ActualizarPacienteRequestDto dto)
    {
        if (idPaciente <= 0)
            throw new ArgumentException("El ID del paciente es inválido.");

        if (dto == null)
            throw new ArgumentNullException(nameof(dto), "Los datos de actualización no pueden estar vacíos.");

        return await _repo.ActualizarPacienteAsync(idPaciente, dto);
    }

    // NUEVO: Método para que recepción busque pacientes por nombre
    public Task<IEnumerable<PacienteResponseDto>> BuscarPorNombreAsync(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El parámetro de búsqueda no puede estar vacío.");

        // Se pasa al repositorio para hacer una búsqueda flexible (ej. usando ILIKE en SQL)
        return _repo.BuscarPorNombreAsync(nombre);
    }
}