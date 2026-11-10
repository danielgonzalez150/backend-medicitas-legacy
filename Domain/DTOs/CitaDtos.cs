using System;

namespace Domain.DTOs;

public class ConfigurarHorarioRequestDto
{
    public int IdDia { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int IntervaloMinutos { get; set; } = 30;
}

public class HorarioResponseDto
{
    public int IdHorario { get; set; }
    public int IdMedico { get; set; }
    public int IdDia { get; set; }
    public string NombreDia { get; set; } = string.Empty;
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int IntervaloMinutos { get; set; }
    public bool Estado { get; set; }
}

public record CrearCitaRequestDto(
    int IdMedico,
    int? IdPaciente,  // Null si agenda el propio paciente tokenizado
    DateOnly Fecha,
    TimeSpan HoraInicio,
    int IdSede = 1    // Id de Sede (por defecto Sede 1)
);

public record ReprogramarCitaRequestDto(
    DateOnly NuevaFecha,
    TimeSpan NuevaHoraInicio
);

public record CambiarEstadoCitaDto(
    int IdEstado      // 2: Atendida, 3: Inasistencia, 4: Cancelada
);

public record CitaDetalleDto(
    int IdCita,
    int IdPaciente,
    string NombrePaciente, // Nombre + Apellido del Paciente
    int IdMedico,
    string NombreMedico,   // Nombre + Apellido del Médico
    string Especialidad,   // Nombre de la especialidad
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    int IdEstado,
    string NombreEstado,   // 'Programada', 'Atendida', etc.
    int IdSede,
    string Lugar          // Nombre + Dirección de la Sede
);