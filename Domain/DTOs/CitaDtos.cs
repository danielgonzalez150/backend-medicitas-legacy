namespace Domain.DTOs;

public class ConfigurarHorarioRequestDto
{
    public int IdDia { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int IntervaloMinutos { get; set; } = 30; // Valor por defecto sugerido
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
    int? IdPaciente, // Será null si el paciente agenda para sí mismo, se llena con el token
    DateOnly Fecha,
    TimeSpan HoraInicio
);
public record ReprogramarCitaRequestDto(
    DateOnly NuevaFecha,
    TimeSpan NuevaHoraInicio
);
public record CambiarEstadoCitaDto(
    int IdEstado // 2: Atendida, 3: Inasistencia, 4: Cancelada
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
    string NombreEstado    // 'Programada', 'Atendida', etc.
);