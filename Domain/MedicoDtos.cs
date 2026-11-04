namespace Domain;

public record MedicoResponseDto(
    int IdMedico,
    int IdUsuario,
    string Nombre,
    string Apellido,
    string TarjetaProfesional,
    string NombreEspecialidad,
    string Correo
);