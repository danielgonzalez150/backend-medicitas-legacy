namespace Domain.DTOs;

public record EspecialidadDto(
    int IdEspecialidad,
    string Nombre,
    string? Descripcion
);