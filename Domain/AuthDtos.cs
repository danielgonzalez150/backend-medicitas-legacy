namespace Domain;

public record LoginRequestDto(string Correo, string Contrasena);

public record AuthResponseDto(int IdUsuario, string Correo, int IdRol);

public class UsuarioDbDto 
{ 
    public int IdUsuario { get; set; } 
    public string Correo { get; set; } = string.Empty; 
    public string ContrasenaHash { get; set; } = string.Empty; 
    public int IdRol { get; set; } 
}