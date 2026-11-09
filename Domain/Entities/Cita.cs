using System;

namespace Domain.Entities;

public class Cita
{
    public int IdCita { get; set; }
    public int IdPaciente { get; set; }
    public int IdMedico { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int IdEstado { get; set; } // 1: Programada, 2: Atendida, 3: Inasistencia, 4: Cancelada
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación (opcionales para mapeos con Dapper/EF)
    public Paciente? Paciente { get; set; }
    public Medico? Medico { get; set; }
    public EstadoCita? Estado { get; set; }
}