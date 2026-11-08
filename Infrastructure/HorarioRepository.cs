using System.Data;
using Dapper;
using Domain;

namespace Infrastructure;

public class HorarioRepository
{
    private readonly IDbConnection _db;

    public HorarioRepository(IDbConnection db) => _db = db;

    // 1. Insertar una nueva configuración de horario
    public async Task<int> GuardarHorarioAsync(int idMedico, ConfigurarHorarioRequestDto dto)
    {
        var sql = @"INSERT INTO horarios_medico 
                        (id_medico, id_dia, hora_inicio, hora_fin, intervalo_minutos, estado)
                    VALUES 
                        (@idMedico, @IdDia, @HoraInicio, @HoraFin, @IntervaloMinutos, true)
                    RETURNING id_horario;";

        return await _db.QuerySingleAsync<int>(sql, new
        {
            idMedico,
            dto.IdDia,
            dto.HoraInicio,
            dto.HoraFin,
            dto.IntervaloMinutos
        });
    }

    // 2. Obtener todos los horarios de un médico (para el panel del administrador/médico)
    public async Task<IEnumerable<HorarioResponseDto>> ObtenerPorMedicoAsync(int idMedico)
    {
        var sql = @"SELECT 
                        h.id_horario AS IdHorario, 
                        h.id_medico AS IdMedico, 
                        h.id_dia AS IdDia, 
                        d.nombre_dia AS NombreDia, 
                        h.hora_inicio AS HoraInicio, 
                        h.hora_fin AS HoraFin, 
                        h.intervalo_minutos AS IntervaloMinutos, 
                        h.estado AS Estado
                    FROM horarios_medico h
                    INNER JOIN dias_semana d ON h.id_dia = d.id_dia
                    WHERE h.id_medico = @idMedico
                    ORDER BY h.id_dia, h.hora_inicio;";

        return await _db.QueryAsync<HorarioResponseDto>(sql, new { idMedico });
    }

    // 3. Obtener el horario de un médico para un día específico (Usado por HorarioService para calcular franjas)
    public async Task<HorarioResponseDto?> ObtenerHorarioPorDiaAsync(int idMedico, int idDia)
    {
        var sql = @"SELECT 
                        h.id_horario AS IdHorario, 
                        h.id_medico AS IdMedico, 
                        h.id_dia AS IdDia, 
                        d.nombre_dia AS NombreDia, 
                        h.hora_inicio AS HoraInicio, 
                        h.hora_fin AS HoraFin, 
                        h.intervalo_minutos AS IntervaloMinutos, 
                        h.estado AS Estado
                    FROM horarios_medico h
                    INNER JOIN dias_semana d ON h.id_dia = d.id_dia
                    WHERE h.id_medico = @idMedico AND h.id_dia = @idDia AND h.estado = true
                    LIMIT 1;";

        return await _db.QueryFirstOrDefaultAsync<HorarioResponseDto>(sql, new { idMedico, idDia });
    }

    // 4. Obtener las horas ya ocupadas (citas programadas) en una fecha específica
    public async Task<IEnumerable<TimeSpan>> ObtenerHorasOcupadasAsync(int idMedico, DateOnly fecha)
    {
        // En PostgreSQL usamos ::date y ::time para extraer partes de un TIMESTAMP
        // Aseguramos que solo cuente las citas en estado 'Programada'
        var sql = @"SELECT c.fecha_hora::time
                    FROM citas c
                    INNER JOIN estados_cita e ON c.id_estado = e.id_estado
                    WHERE c.id_medico = @idMedico 
                      AND c.fecha_hora::date = @fecha 
                      AND e.nombre_estado = 'Programada';";

        // Dapper mapeará automáticamente el ::time de PostgreSQL a TimeSpan en C#
        return await _db.QueryAsync<TimeSpan>(sql, new { idMedico, fecha });
    }
    
    // 5. (Opcional pero recomendado) Actualizar el estado de un horario (Activarlo/Desactivarlo)
    public async Task<bool> ActualizarEstadoHorarioAsync(int idHorario, bool estado)
    {
        var sql = @"UPDATE horarios_medico 
                    SET estado = @estado 
                    WHERE id_horario = @idHorario;";
                    
        var filasAfectadas = await _db.ExecuteAsync(sql, new { idHorario, estado });
        return filasAfectadas > 0;
    }
}