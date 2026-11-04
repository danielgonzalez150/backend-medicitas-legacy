namespace Domain;

public record RegistroPacienteRequestDto(
    string Correo,
    string Contrasena,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    string Apellido,
    string Telefono,
    DateTime FechaNacimiento
);

public class PacienteResponseDto
{
    public int IdPaciente { get; set; }
    public int IdUsuario { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string Correo { get; set; } = string.Empty;
}