namespace Domain.DTOs;

public class CrearMedicoRequestDto
{
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public int IdEspecialidad { get; set; }
    public string TarjetaProfesional { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
}

public record MedicoResponseDto(
    int IdMedico,
    int IdUsuario,
    string Nombre,
    string Apellido,
    string TarjetaProfesional,
    string NombreEspecialidad,
    string Correo
);

public class DesactivarMedicoRequestDto
{
    // Permite tanto activar como desactivar (true/false)
    public bool Estado { get; set; } 
}