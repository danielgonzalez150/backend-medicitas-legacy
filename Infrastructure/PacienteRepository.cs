using System.Data;
using Dapper;
using Domain;

namespace Infrastructure;

public class PacienteRepository
{
    private readonly IDbConnection _db;
    
    public PacienteRepository(IDbConnection db) => _db = db;

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
        var sql = @"SELECT p.id_paciente AS IdPaciente, p.id_usuario AS IdUsuario, p.tipo_documento AS TipoDocumento, 
                           p.numero_documento AS NumeroDocumento, p.nombre AS Nombre, p.apellido AS Apellido, 
                           p.telefono AS Telefono, p.fecha_nacimiento AS FechaNacimiento, u.correo AS Correo
                    FROM pacientes p JOIN usuarios u ON p.id_usuario = u.id_usuario";
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
}