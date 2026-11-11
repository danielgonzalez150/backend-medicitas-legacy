using System;

namespace Domain.DTOs;

public class ConfigurarHorarioRequestDto
{
    public int IdDia { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int IntervaloMinutos { get; set; } = 30;
}

public class HorarioResponseDto
{
    public int IdHorario { get; set; }
    public int IdMedico { get; set; }
    public int IdDia { get; set; }
    public string NombreDia { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int IntervaloMinutos { get; set; }
    public bool Estado { get; set; }
}

public record CrearCitaRequestDto(
    int IdMedico,
    int? IdPaciente,  // Null si agenda el propio paciente tokenizado
    DateOnly Fecha,
    TimeOnly HoraInicio,
    int IdSede = 1    // Id de Sede (por defecto Sede 1)
);

public record ReprogramarCitaRequestDto(
    DateOnly NuevaFecha,
    TimeOnly NuevaHoraInicio
);

public record CambiarEstadoCitaDto(
    int IdEstado      // 2: Atendida, 3: Inasistencia, 4: Cancelada
);

// DTO de respuesta para disponibilidades
public class FechaHoraOcupadaDto
{
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
}

// DTO de detalle de cita refactorizado a clase estándar para Dapper
public class CitaDetalleDto
{
    public int IdCita { get; set; }
    public int IdPaciente { get; set; }
    public string NombrePaciente { get; set; } = string.Empty;
    public int IdMedico { get; set; }
    public string NombreMedico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int IdEstado { get; set; }
    public string NombreEstado { get; set; } = string.Empty;
    public int IdSede { get; set; }
    public string Lugar { get; set; } = string.Empty;
}