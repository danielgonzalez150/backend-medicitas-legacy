using System.Data;
using Dapper;
using System.Threading.Tasks;
using Domain.DTOs;

namespace Infrastructure.Repositories;

public class UsuarioRepository
{
    private readonly IDbConnection _db;
    
    public UsuarioRepository(IDbConnection db) => _db = db;

    public async Task<UsuarioDbDto?> ObteneporCorreoAsync(string correo)
    {
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
            WHERE u.correo = @correo AND u.estado = true";
            
        return await _db.QueryFirstOrDefaultAsync<UsuarioDbDto>(sql, new { correo });
    }

    public async Task<bool> ExisteCorreoAsync(string correo, IDbTransaction transaction)
    {
        var sql = "SELECT EXISTS(SELECT 1 FROM usuarios WHERE correo = @correo)";
        return await _db.ExecuteScalarAsync<bool>(sql, new { correo }, transaction);
    }
}