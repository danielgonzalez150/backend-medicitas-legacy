using Dapper;
using Domain.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class CitaRepository
{
    private readonly string _connectionString;

    public CitaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    public async Task<int> CrearAsync(int idMedico, int idPaciente, DateOnly fecha, TimeSpan horaInicio)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var horaFin = horaInicio.Add(TimeSpan.FromMinutes(30));

        var sql = @"
            INSERT INTO citas (
                id_medico, 
                id_paciente, 
                fecha, 
                hora_inicio, 
                hora_fin, 
                id_estado
            ) 
            VALUES (
                @IdMedico, 
                @IdPaciente, 
                @Fecha, 
                @HoraInicio, 
                @HoraFin, 
                1 -- Estado 1 = 'Programada'
            ) 
            RETURNING id_cita;";

        var parameters = new
        {
            IdMedico = idMedico,
            IdPaciente = idPaciente,
            Fecha = fecha,
            HoraInicio = horaInicio,
            HoraFin = horaFin
        };

        var idCita = await connection.ExecuteScalarAsync<int>(sql, parameters);
        return idCita;
    }

    public async Task<IEnumerable<TimeSpan>> ObtenerHorasOcupadasAsync(int idMedico, DateOnly fecha)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT hora_inicio 
            FROM citas 
            WHERE id_medico = @IdMedico 
              AND fecha = @Fecha 
              AND id_estado = 1; -- 1: Programada";

        var horasOcupadas = await connection.QueryAsync<TimeSpan>(sql, new { IdMedico = idMedico, Fecha = fecha });
        return horasOcupadas;
    }

    public async Task<IEnumerable<CitaDetalleDto>> ObtenerPorPacienteAsync(int idPaciente, DateOnly fechaInicio, DateOnly fechaFin)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT 
                c.id_cita AS IdCita,
                c.id_paciente AS IdPaciente,
                (up.nombre || ' ' || up.apellido) AS NombrePaciente,
                c.id_medico AS IdMedico,
                (um.nombre || ' ' || um.apellido) AS NombreMedico,
                e.nombre AS Especialidad,
                c.fecha AS Fecha,
                c.hora_inicio AS HoraInicio,
                c.hora_fin AS HoraFin,
                c.id_estado AS IdEstado,
                ec.nombre_estado AS NombreEstado
            FROM citas c
            INNER JOIN usuarios up ON c.id_paciente = up.id_usuario
            INNER JOIN usuarios um ON c.id_medico = um.id_usuario
            INNER JOIN medicos m ON c.id_medico = m.id_medico
            INNER JOIN especialidades e ON m.id_especialidad = e.id_especialidad
            INNER JOIN estados_cita ec ON c.id_estado = ec.id_estado
            WHERE c.id_paciente = @IdPaciente
              AND c.fecha BETWEEN @FechaInicio AND @FechaFin
            ORDER BY c.fecha ASC, c.hora_inicio ASC;";

        return await connection.QueryAsync<CitaDetalleDto>(sql, new { IdPaciente = idPaciente, FechaInicio = fechaInicio, FechaFin = fechaFin });
    }

    public async Task<IEnumerable<CitaDetalleDto>> ObtenerPorMedicoAsync(int idMedico, DateOnly fechaInicio, DateOnly fechaFin)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT 
                c.id_cita AS IdCita,
                c.id_paciente AS IdPaciente,
                (up.nombre || ' ' || up.apellido) AS NombrePaciente,
                c.id_medico AS IdMedico,
                (um.nombre || ' ' || um.apellido) AS NombreMedico,
                e.nombre AS Especialidad,
                c.fecha AS Fecha,
                c.hora_inicio AS HoraInicio,
                c.hora_fin AS HoraFin,
                c.id_estado AS IdEstado,
                ec.nombre_estado AS NombreEstado
            FROM citas c
            INNER JOIN usuarios up ON c.id_paciente = up.id_usuario
            INNER JOIN usuarios um ON c.id_medico = um.id_usuario
            INNER JOIN medicos m ON c.id_medico = m.id_medico
            INNER JOIN especialidades e ON m.id_especialidad = e.id_especialidad
            INNER JOIN estados_cita ec ON c.id_estado = ec.id_estado
            WHERE c.id_medico = @IdMedico
              AND c.fecha BETWEEN @FechaInicio AND @FechaFin
            ORDER BY c.fecha ASC, c.hora_inicio ASC;";

        return await connection.QueryAsync<CitaDetalleDto>(sql, new { IdMedico = idMedico, FechaInicio = fechaInicio, FechaFin = fechaFin });
    }

    public async Task<Cita?> ObtenerPorIdAsync(int idCita)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            SELECT 
                id_cita AS IdCita,
                id_paciente AS IdPaciente,
                id_medico AS IdMedico,
                fecha AS Fecha,
                hora_inicio AS HoraInicio,
                hora_fin AS HoraFin,
                id_estado AS IdEstado,
                fecha_creacion AS FechaCreacion
            FROM citas
            WHERE id_cita = @IdCita;";

        return await connection.QueryFirstOrDefaultAsync<Cita>(sql, new { IdCita = idCita });
    }

    public async Task<bool> ActualizarEstadoAsync(int idCita, int idEstado)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = @"
            UPDATE citas 
            SET id_estado = @IdEstado 
            WHERE id_cita = @IdCita;";

        var filasAfectadas = await connection.ExecuteAsync(sql, new { IdCita = idCita, IdEstado = idEstado });
        return filasAfectadas > 0;
    }

    public async Task<bool> ReprogramarAsync(int idCita, DateOnly nuevaFecha, TimeSpan nuevaHoraInicio)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var nuevaHoraFin = nuevaHoraInicio.Add(TimeSpan.FromMinutes(30));

        var sql = @"
            UPDATE citas 
            SET fecha = @NuevaFecha,
                hora_inicio = @NuevaHoraInicio,
                hora_fin = @NuevaHoraFin,
                id_estado = 1 -- Garantiza que permanezca en estado 'Programada'
            WHERE id_cita = @IdCita;";

        var filasAfectadas = await connection.ExecuteAsync(sql, new 
        { 
            IdCita = idCita, 
            NuevaFecha = nuevaFecha, 
            NuevaHoraInicio = nuevaHoraInicio, 
            NuevaHoraFin = nuevaHoraFin 
        });

        return filasAfectadas > 0;
    }
}