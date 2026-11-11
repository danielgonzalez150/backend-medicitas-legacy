using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class HorarioRepository
{
    private readonly string _connectionString;

    public HorarioRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    // 1. Insertar una nueva configuración de horario
    public async Task<int> GuardarHorarioAsync(int idMedico, ConfigurarHorarioRequestDto dto)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"INSERT INTO horarios_medico 
                        (id_medico, id_dia, hora_inicio, hora_fin, intervalo_minutos, estado)
                    VALUES 
                        (@IdMedico, @IdDia, @HoraInicio, @HoraFin, @IntervaloMinutos, true)
                    RETURNING id_horario;";

        return await connection.QuerySingleAsync<int>(sql, new
        {
            IdMedico = idMedico,
            dto.IdDia,
            dto.HoraInicio,
            dto.HoraFin,
            dto.IntervaloMinutos
        });
    }

    // 2. Obtener todos los horarios de un médico
    public async Task<IEnumerable<HorarioResponseDto>> ObtenerPorMedicoAsync(int idMedico)
    {
        using var connection = new NpgsqlConnection(_connectionString);

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
                    WHERE h.id_medico = @IdMedico
                    ORDER BY h.id_dia, h.hora_inicio;";

        return await connection.QueryAsync<HorarioResponseDto>(sql, new { IdMedico = idMedico });
    }

    // 3. Obtener el horario de un médico para un día específico
    public async Task<HorarioResponseDto?> ObtenerHorarioPorDiaAsync(int idMedico, int idDia)
    {
        using var connection = new NpgsqlConnection(_connectionString);

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
                    WHERE h.id_medico = @IdMedico AND h.id_dia = @IdDia AND h.estado = true
                    LIMIT 1;";

        return await connection.QueryFirstOrDefaultAsync<HorarioResponseDto>(sql, new { IdMedico = idMedico, IdDia = idDia });
    }

    // 4. Obtener las horas ya ocupadas en una fecha específica
    // Cambia la firma de IEnumerable<TimeSpan> a IEnumerable<TimeOnly>
    public async Task<IEnumerable<TimeOnly>> ObtenerHorasOcupadasAsync(int idMedico, DateOnly fecha)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"SELECT hora_inicio 
                    FROM citas 
                    WHERE id_medico = @IdMedico 
                    AND fecha = @Fecha 
                    AND id_estado = 1;";

        return await connection.QueryAsync<TimeOnly>(sql, new { IdMedico = idMedico, Fecha = fecha });
    }
    
    // 5. Actualizar el estado de un horario (Activar/Desactivar)
    public async Task<bool> ActualizarEstadoHorarioAsync(int idHorario, bool estado)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"UPDATE horarios_medico 
                    SET estado = @Estado 
                    WHERE id_horario = @IdHorario;";
                    
        var filasAfectadas = await connection.ExecuteAsync(sql, new { IdHorario = idHorario, Estado = estado });
        return filasAfectadas > 0;
    }
}