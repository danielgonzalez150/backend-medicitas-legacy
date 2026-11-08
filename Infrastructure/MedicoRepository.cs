using System.Data;
using Dapper;
using Domain;

namespace Infrastructure;

public class MedicoRepository
{
    private readonly IDbConnection _db;
    
    public MedicoRepository(IDbConnection db) => _db = db;

    public async Task<IEnumerable<MedicoResponseDto>> ObtenerTodosAsync()
    {
        var sql = @"SELECT m.id_medico AS IdMedico, m.id_usuario AS IdUsuario, m.nombre AS Nombre, 
                           m.apellido AS Apellido, m.tarjeta_profesional AS TarjetaProfesional, 
                           e.nombre_especialidad AS NombreEspecialidad, u.correo AS Correo
                    FROM medicos m
                    JOIN especialidades e ON m.id_especialidad = e.id_especialidad
                    JOIN usuarios u ON m.id_usuario = u.id_usuario";
        return await _db.QueryAsync<MedicoResponseDto>(sql);
    }

    public async Task<int> CrearMedicoTransaccionalAsync(CrearMedicoRequestDto dto, string hashClave)
    {
        if (_db.State != ConnectionState.Open) _db.Open();
        using var transaction = _db.BeginTransaction();
        try
        {
            // id_rol = 2 corresponde a 'Medico' en tu tabla roles
            var sqlUsuario = @"INSERT INTO usuarios (correo, contrasena_hash, id_rol, estado) 
                               VALUES (@Correo, @Hash, 2, true) RETURNING id_usuario;";
            var idUsuario = await _db.QuerySingleAsync<int>(sqlUsuario, new { dto.Correo, Hash = hashClave }, transaction);

            var sqlMedico = @"INSERT INTO medicos (id_usuario, id_especialidad, tarjeta_profesional, nombre, apellido)
                              VALUES (@idUsuario, @IdEspecialidad, @TarjetaProfesional, @Nombre, @Apellido) RETURNING id_medico;";
            
            var idMedico = await _db.QuerySingleAsync<int>(sqlMedico, new {
                idUsuario, dto.IdEspecialidad, dto.TarjetaProfesional, dto.Nombre, dto.Apellido
            }, transaction);

            transaction.Commit();
            return idMedico;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> TieneCitasPendientesAsync(int idMedico)
    {
        // Se hace un JOIN con estados_cita ya que el nombre del estado está normalizado en esa tabla
        var sql = @"SELECT COUNT(1) 
                    FROM citas c
                    INNER JOIN estados_cita e ON c.id_estado = e.id_estado
                    WHERE c.id_medico = @idMedico AND e.nombre_estado = 'Programada';";
        
        var count = await _db.ExecuteScalarAsync<int>(sql, new { idMedico });
        return count > 0;
    }

    public async Task<bool> ActualizarEstadoAsync(int idMedico, bool estado)
    {
        // El campo 'estado' (activo/inactivo) pertenece a la tabla usuarios, 
        // así que lo actualizamos buscando el usuario asociado a este id_medico.
        var sql = @"UPDATE usuarios 
                    SET estado = @estado 
                    WHERE id_usuario = (SELECT id_usuario FROM medicos WHERE id_medico = @idMedico);";
        
        var filasAfectadas = await _db.ExecuteAsync(sql, new { idMedico, estado });
        return filasAfectadas > 0;
    }
}