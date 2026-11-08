using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Infrastructure;

public class CitaRepository
{
    private readonly string _connectionString;

    public CitaRepository(IConfiguration configuration)
    {
        // Se lee la cadena de conexión configurada en Program.cs o appsettings.json
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    public async Task<int> CrearAsync(int idMedico, int idPaciente, DateOnly fecha, TimeSpan horaInicio)
    {
        // Dapper maneja automáticamente la apertura y cierre de la conexión dentro del bloque 'using'
        using var connection = new NpgsqlConnection(_connectionString);

        // Por defecto, asumimos que cada cita dura 30 minutos. 
        // Si tu sistema tiene duraciones variables por médico, este valor debería venir desde CitaService.
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
                1 -- Estado 1 = 'Programada' (según tu tabla estados_cita)
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

        // ExecuteScalarAsync ejecuta el INSERT y retorna la primera columna de la primera fila (id_cita)
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
}