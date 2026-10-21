using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Dapper;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS para permitir el frontend local o desplegado
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles(); // Habilita servir el index.html desde la carpeta wwwroot

app.MapPost("/api/registro", async (RegistroRequest req, IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("SupabaseDB");
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var transaction = connection.BeginTransaction();
    try
    {
        // 1. Hashear contraseña
        string hash = BCrypt.Net.BCrypt.HashPassword(req.Contrasena);

        // 2. Insertar Usuario (id_rol = 3 para Paciente)
        var sqlUsuario = @"
            INSERT INTO usuarios (correo, contrasena_hash, id_rol, estado) 
            VALUES (@Correo, @ContrasenaHash, 3, true) RETURNING id_usuario;";
            
        var idUsuario = await connection.QuerySingleAsync<int>(sqlUsuario, new { 
            Correo = req.Correo, 
            ContrasenaHash = hash 
        }, transaction);

        // 3. Insertar Paciente
        var sqlPaciente = @"
            INSERT INTO pacientes (id_usuario, tipo_documento, numero_documento, nombre, apellido, telefono, fecha_nacimiento)
            VALUES (@IdUsuario, @TipoDocumento, @NumeroDocumento, @Nombre, @Apellido, @Telefono, @FechaNacimiento);";
            
        await connection.ExecuteAsync(sqlPaciente, new {
            IdUsuario = idUsuario,
            req.TipoDocumento,
            req.NumeroDocumento,
            req.Nombre,
            req.Apellido,
            req.Telefono,
            req.FechaNacimiento
        }, transaction);

        transaction.Commit();
        return Results.Ok(new { message = "Registro exitoso" });
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

// Modelo de datos para la petición
public class RegistroRequest
{
    public string Correo { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public DateTime FechaNacimiento { get; set; }
}