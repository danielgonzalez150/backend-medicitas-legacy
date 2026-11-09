using System;

namespace Domain.Entities;

public class Usuario
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;

    // Navegación
    public Rol? Rol { get; set; }
}