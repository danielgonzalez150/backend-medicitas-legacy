using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class UsuarioRepository
{
    private readonly string _connectionString;

    public UsuarioRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    public async Task<UsuarioDbDto?> ObtenerPorCorreoAsync(string correo)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT 
                u.id_usuario AS IdUsuario, 
                u.correo AS Correo, 
                u.contrasena_hash AS ContrasenaHash, 
                u.id_rol AS IdRol, 
                u.estado AS Estado,
                p.id_paciente AS IdPaciente,
                m.id_medico AS IdMedico
            FROM usuarios u
            LEFT JOIN pacientes p ON u.id_usuario = p.id_usuario
            LEFT JOIN medicos m ON u.id_usuario = m.id_usuario
            WHERE u.correo = @Correo AND u.estado = true;";
            
        return await connection.QueryFirstOrDefaultAsync<UsuarioDbDto>(sql, new { Correo = correo });
    }

    public async Task<bool> ExisteCorreoAsync(string correo, IDbTransaction? transaction = null)
    {
        var sql = "SELECT EXISTS(SELECT 1 FROM usuarios WHERE correo = @Correo);";

        if (transaction != null)
        {
            return await transaction.Connection!.ExecuteScalarAsync<bool>(sql, new { Correo = correo }, transaction);
        }

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(sql, new { Correo = correo });
    }
}