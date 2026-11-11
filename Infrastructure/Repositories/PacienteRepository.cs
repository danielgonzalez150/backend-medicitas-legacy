using System.Data;
using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Infrastructure.Repositories;

public class PacienteRepository
{
    private readonly IDbConnection _db;
    
    public PacienteRepository(IConfiguration configuration)
    {
        _db = new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada."));
    }

    public async Task<int> CrearPacienteTransaccionalAsync(RegistroPacienteRequestDto dto, string hashClave)
    {
        if (_db.State != ConnectionState.Open) _db.Open();
        using var transaction = _db.BeginTransaction();
        try
        {
            var sqlUsuario = @"INSERT INTO usuarios (correo, contrasena_hash, id_rol, estado) 
                               VALUES (@Correo, @Hash, 3, true) RETURNING id_usuario;";
            var idUsuario = await _db.QuerySingleAsync<int>(sqlUsuario, new { dto.Correo, Hash = hashClave }, transaction);

            var sqlPaciente = @"INSERT INTO pacientes (id_usuario, tipo_documento, numero_documento, nombre, apellido, telefono, fecha_nacimiento)
                                VALUES (@idUsuario, @TipoDocumento, @NumeroDocumento, @Nombre, @Apellido, @Telefono, @FechaNacimiento);";
            
            await _db.ExecuteAsync(sqlPaciente, new {
                idUsuario, dto.TipoDocumento, dto.NumeroDocumento, dto.Nombre, dto.Apellido, dto.Telefono, dto.FechaNacimiento
            }, transaction);

            transaction.Commit();
            return idUsuario;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<PacienteResponseDto>> ObtenerTodosAsync()
    {
        const string sql = @"
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

        return await _db.QueryAsync<PacienteResponseDto>(sql);
    }

    public async Task<PacienteResponseDto?> ObtenerPorDocumentoAsync(string documento)
    {
        var sql = @"SELECT p.id_paciente AS IdPaciente, p.id_usuario AS IdUsuario, p.tipo_documento AS TipoDocumento, 
                           p.numero_documento AS NumeroDocumento, p.nombre AS Nombre, p.apellido AS Apellido, 
                           p.telefono AS Telefono, p.fecha_nacimiento AS FechaNacimiento, u.correo AS Correo
                    FROM pacientes p JOIN usuarios u ON p.id_usuario = u.id_usuario WHERE p.numero_documento = @documento";
        return await _db.QueryFirstOrDefaultAsync<PacienteResponseDto>(sql, new { documento });
    }

    // NUEVO: Método para actualizar el perfil del paciente
    public async Task<bool> ActualizarPacienteAsync(int idPaciente, ActualizarPacienteRequestDto dto)
    {
        var sql = @"UPDATE pacientes 
                    SET tipo_documento = @TipoDocumento, 
                        numero_documento = @NumeroDocumento, 
                        nombre = @Nombre, 
                        apellido = @Apellido, 
                        telefono = @Telefono, 
                        fecha_nacimiento = @FechaNacimiento
                    WHERE id_paciente = @IdPaciente;";
        
        var filasAfectadas = await _db.ExecuteAsync(sql, new { 
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

    // NUEVO: Método para búsqueda flexible por nombre o apellido
    public async Task<IEnumerable<PacienteResponseDto>> BuscarPorNombreAsync(string termino)
    {
        var sql = @"SELECT p.id_paciente AS IdPaciente, p.id_usuario AS IdUsuario, p.tipo_documento AS TipoDocumento, 
                           p.numero_documento AS NumeroDocumento, p.nombre AS Nombre, p.apellido AS Apellido, 
                           p.telefono AS Telefono, p.fecha_nacimiento AS FechaNacimiento, u.correo AS Correo
                    FROM pacientes p 
                    JOIN usuarios u ON p.id_usuario = u.id_usuario 
                    WHERE p.nombre ILIKE @termino OR p.apellido ILIKE @termino;";
        
        // Se agregan los comodines % para buscar coincidencias parciales en cualquier parte del texto
        return await _db.QueryAsync<PacienteResponseDto>(sql, new { termino = $"%{termino}%" });
    }
}