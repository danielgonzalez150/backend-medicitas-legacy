namespace Domain.DTOs;

public record LoginRequestDto(string Correo, string Contrasena);

public record AuthResponseDto(int IdUsuario, string Correo, int IdRol, string Token);

public class UsuarioDbDto 
{ 
    public int IdUsuario { get; set; } 
    public string Correo { get; set; } = string.Empty; 
    public string ContrasenaHash { get; set; } = string.Empty; 
    public int IdRol { get; set; } 
    public bool Estado { get; set; }    
    public int? IdPaciente { get; set; } // Agrega esta línea
    public int? IdMedico { get; set; }   // Agrega esta línea
}