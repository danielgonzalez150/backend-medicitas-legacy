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
}