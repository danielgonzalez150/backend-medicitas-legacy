namespace Domain.DTOs;

public class RegistroPacienteRequestDto
{
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
}

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

public class ActualizarPacienteRequestDto
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
}