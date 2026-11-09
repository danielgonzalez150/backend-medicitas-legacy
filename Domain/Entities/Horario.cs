using System;

namespace Domain.Entities;

public class Horario
{
    public int IdHorario { get; set; }
    public int IdMedico { get; set; }
    public int DiaSemana { get; set; } // 0 = Domingo, 1 = Lunes, ..., 6 = Sábado
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int IntervaloMinutos { get; set; } // Ejemplo: 30 minutos por cita

    // Navegación
    public Medico? Medico { get; set; }
}