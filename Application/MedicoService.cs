using Domain;
using Infrastructure;
using BCrypt.Net;
using System;
using System.Threading.Tasks;

namespace Application;

public class MedicoService
{
    private readonly MedicoRepository _repo;

    public MedicoService(MedicoRepository repo)
    {
        _repo = repo;
    }

    public async Task<int> RegistrarMedicoAsync(CrearMedicoRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Correo) || string.IsNullOrWhiteSpace(dto.Contrasena))
            throw new ArgumentException("El correo y la contraseña son obligatorios.");

        // Encriptar la contraseña antes de enviarla a la base de datos
        string hash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena);
        
        // El repositorio se encargará de la transacción (crear usuario + crear médico)
        return await _repo.CrearMedicoTransaccionalAsync(dto, hash);
    }

    public async Task<bool> DesactivarMedicoAsync(int idMedico, DesactivarMedicoRequestDto dto)
    {
        if (idMedico <= 0)
            throw new ArgumentException("El ID del médico es inválido.");

        // Regla de negocio del Sprint 1: Si se intenta desactivar (Estado = false)
        if (!dto.Estado)
        {
            // Validar que no tenga citas pendientes o programadas
            bool tieneCitasPendientes = await _repo.TieneCitasPendientesAsync(idMedico);
            if (tieneCitasPendientes)
            {
                throw new InvalidOperationException("No se puede desactivar al médico porque tiene citas programadas pendientes de reasignación.");
            }
        }

        return await _repo.ActualizarEstadoAsync(idMedico, dto.Estado);
    }
}