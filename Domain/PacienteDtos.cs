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

public record PacienteResponseDto(
    int IdPaciente,
    int IdUsuario,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    string Apellido,
    string Telefono,
    DateTime FechaNacimiento,
    string Correo
);