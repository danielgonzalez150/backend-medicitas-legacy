using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class MedicoRepository
{
    private readonly string _connectionString;

    public MedicoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    public async Task<IEnumerable<MedicoResponseDto>> ObtenerTodosAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"SELECT m.id_medico AS IdMedico, 
                           m.id_usuario AS IdUsuario, 
                           m.nombre AS Nombre, 
                           m.apellido AS Apellido, 
                           m.tarjeta_profesional AS TarjetaProfesional, 
                           e.nombre_especialidad AS NombreEspecialidad, 
                           u.correo AS Correo
                    FROM medicos m
                    INNER JOIN especialidades e ON m.id_especialidad = e.id_especialidad
                    INNER JOIN usuarios u ON m.id_usuario = u.id_usuario;";

        return await connection.QueryAsync<MedicoResponseDto>(sql);
    }

    public async Task<int> CrearMedicoTransaccionalAsync(CrearMedicoRequestDto dto, string hashClave)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // id_rol = 2 corresponde a 'Medico' en la tabla roles
            var sqlUsuario = @"INSERT INTO usuarios (correo, contrasena_hash, id_rol, estado) 
                               VALUES (@Correo, @Hash, 2, true) 
                               RETURNING id_usuario;";

            var idUsuario = await connection.QuerySingleAsync<int>(sqlUsuario, new { dto.Correo, Hash = hashClave }, transaction);

            var sqlMedico = @"INSERT INTO medicos (id_usuario, id_especialidad, tarjeta_profesional, nombre, apellido)
                              VALUES (@IdUsuario, @IdEspecialidad, @TarjetaProfesional, @Nombre, @Apellido) 
                              RETURNING id_medico;";
            
            var idMedico = await connection.QuerySingleAsync<int>(sqlMedico, new {
                IdUsuario = idUsuario, 
                dto.IdEspecialidad, 
                dto.TarjetaProfesional, 
                dto.Nombre, 
                dto.Apellido
            }, transaction);

            await transaction.CommitAsync();
            return idMedico;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> TieneCitasPendientesAsync(int idMedico)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"SELECT COUNT(1) 
                    FROM citas c
                    INNER JOIN estados_cita e ON c.id_estado = e.id_estado
                    WHERE c.id_medico = @IdMedico AND e.nombre_estado = 'Programada';";
        
        var count = await connection.ExecuteScalarAsync<int>(sql, new { IdMedico = idMedico });
        return count > 0;
    }

    public async Task<bool> ActualizarEstadoAsync(int idMedico, bool estado)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // El campo 'estado' pertenece a la tabla usuarios
        var sql = @"UPDATE usuarios 
                    SET estado = @Estado 
                    WHERE id_usuario = (SELECT id_usuario FROM medicos WHERE id_medico = @IdMedico);";
        
        var filasAfectadas = await connection.ExecuteAsync(sql, new { IdMedico = idMedico, Estado = estado });
        return filasAfectadas > 0;
    }
}