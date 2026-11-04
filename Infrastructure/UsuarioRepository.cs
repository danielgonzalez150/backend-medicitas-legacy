using System.Data;
using Dapper;
using Domain;

namespace Infrastructure;

public class UsuarioRepository
{
    private readonly IDbConnection _db;
    
    public UsuarioRepository(IDbConnection db) => _db = db;

    public async Task<UsuarioDbDto?> ObteneporCorreoAsync(string correo)
    {
        var sql = "SELECT id_usuario AS IdUsuario, correo AS Correo, contrasena_hash AS ContrasenaHash, id_rol AS IdRol FROM usuarios WHERE correo = @correo AND estado = true";
        return await _db.QueryFirstOrDefaultAsync<UsuarioDbDto>(sql, new { correo });
    }

    public async Task<bool> ExisteCorreoAsync(string correo, IDbTransaction transaction)
    {
        var sql = "SELECT EXISTS(SELECT 1 FROM usuarios WHERE correo = @correo)";
        return await _db.ExecuteScalarAsync<bool>(sql, new { correo }, transaction);
    }
}