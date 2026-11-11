using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class PacienteRepository
{
    private readonly string _connectionString;

    public PacienteRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    public async Task<int> CrearPacienteTransaccionalAsync(RegistroPacienteRequestDto dto, string hashClave)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // id_rol = 3 corresponde a 'Paciente' en la tabla roles
            var sqlUsuario = @"INSERT INTO usuarios (correo, contrasena_hash, id_rol, estado) 
                               VALUES (@Correo, @Hash, 3, true) 
                               RETURNING id_usuario;";

            var idUsuario = await connection.QuerySingleAsync<int>(sqlUsuario, new { dto.Correo, Hash = hashClave }, transaction);

            var sqlPaciente = @"INSERT INTO pacientes (id_usuario, tipo_documento, numero_documento, nombre, apellido, telefono, fecha_nacimiento)
                                VALUES (@IdUsuario, @TipoDocumento, @NumeroDocumento, @Nombre, @Apellido, @Telefono, @FechaNacimiento);";
            
            await connection.ExecuteAsync(sqlPaciente, new {
                IdUsuario = idUsuario, 
                dto.TipoDocumento, 
                dto.NumeroDocumento, 
                dto.Nombre, 
                dto.Apellido, 
                dto.Telefono, 
                dto.FechaNacimiento
            }, transaction);

            await transaction.CommitAsync();
            return idUsuario;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<PacienteResponseDto>> ObtenerTodosAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT 
                p.id_paciente AS IdPaciente,
                p.id_usuario AS IdUsuario,
                p.tipo_documento AS TipoDocumento,
                p.numero_documento AS NumeroDocumento,
                p.nombre AS Nombre,
                p.apellido AS Apellido,
                p.telefono AS Telefono,
                p.fecha_nacimiento AS FechaNacimiento,
                u.correo AS Correo
            FROM pacientes p
            INNER JOIN usuarios u ON p.id_usuario = u.id_usuario;";

        return await connection.QueryAsync<PacienteResponseDto>(sql);
    }

    public async Task<PacienteResponseDto?> ObtenerPorDocumentoAsync(string documento)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"SELECT 
                        p.id_paciente AS IdPaciente, 
                        p.id_usuario AS IdUsuario, 
                        p.tipo_documento AS TipoDocumento, 
                        p.numero_documento AS NumeroDocumento, 
                        p.nombre AS Nombre, 
                        p.apellido AS Apellido, 
                        p.telefono AS Telefono, 
                        p.fecha_nacimiento AS FechaNacimiento, 
                        u.correo AS Correo
                    FROM pacientes p 
                    INNER JOIN usuarios u ON p.id_usuario = u.id_usuario 
                    WHERE p.numero_documento = @Documento;";

        return await connection.QueryFirstOrDefaultAsync<PacienteResponseDto>(sql, new { Documento = documento });
    }

    public async Task<bool> ActualizarPacienteAsync(int idPaciente, ActualizarPacienteRequestDto dto)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"UPDATE pacientes 
                    SET tipo_documento = @TipoDocumento, 
                        numero_documento = @NumeroDocumento, 
                        nombre = @Nombre, 
                        apellido = @Apellido, 
                        telefono = @Telefono, 
                        fecha_nacimiento = @FechaNacimiento
                    WHERE id_paciente = @IdPaciente;";
        
        var filasAfectadas = await connection.ExecuteAsync(sql, new { 
            dto.TipoDocumento, 
            dto.NumeroDocumento, 
            dto.Nombre, 
            dto.Apellido, 
            dto.Telefono, 
            dto.FechaNacimiento,
            IdPaciente = idPaciente
        });
        
        return filasAfectadas > 0;
    }

    public async Task<IEnumerable<PacienteResponseDto>> BuscarPorNombreAsync(string termino)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"SELECT 
                        p.id_paciente AS IdPaciente, 
                        p.id_usuario AS IdUsuario, 
                        p.tipo_documento AS TipoDocumento, 
                        p.numero_documento AS NumeroDocumento, 
                        p.nombre AS Nombre, 
                        p.apellido AS Apellido, 
                        p.telefono AS Telefono, 
                        p.fecha_nacimiento AS FechaNacimiento, 
                        u.correo AS Correo
                    FROM pacientes p 
                    INNER JOIN usuarios u ON p.id_usuario = u.id_usuario 
                    WHERE p.nombre ILIKE @Termino OR p.apellido ILIKE @Termino;";
        
        return await connection.QueryAsync<PacienteResponseDto>(sql, new { Termino = $"%{termino}%" });
    }
}