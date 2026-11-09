using System;

namespace Domain.Entities;

public class Paciente
{
    public int IdPaciente { get; set; } // FK a Usuario.IdUsuario
    public DateOnly FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public string? HistorialMedico { get; set; }

    // Navegación
    public Usuario? Usuario { get; set; }
}