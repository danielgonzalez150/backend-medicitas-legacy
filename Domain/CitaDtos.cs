namespace Domain;

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