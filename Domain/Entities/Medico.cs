namespace Domain.Entities;

public class Medico
{
    public int IdMedico { get; set; } // FK a Usuario.IdUsuario
    public int IdEspecialidad { get; set; }
    public string NumeroColegiado { get; set; } = string.Empty;
    public int ExperienciaAnios { get; set; }

    // Navegación
    public Usuario? Usuario { get; set; }
    public Especialidad? Especialidad { get; set; }
}