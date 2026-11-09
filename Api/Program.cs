using Application;
using Application.Validators;
using Infrastructure;
using Api.Endpoints;
using Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddDatabaseConfiguration(builder.Configuration);

// 1. Configuración de Autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSuperSecretaDeDesarrolloMediCitas12345!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Habilitar Autorización basada en roles y políticas
builder.Services.AddAuthorization();

// 2. Inyección de Repositorios (Infrastructure)
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<PacienteRepository>();
builder.Services.AddScoped<MedicoRepository>();
builder.Services.AddScoped<HorarioRepository>(); // NUEVO
builder.Services.AddScoped<CitaRepository>();    // NUEVO

// 3. Inyección de Servicios (Application)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PacienteService>();
builder.Services.AddScoped<MedicoService>();
builder.Services.AddScoped<HorarioService>();
builder.Services.AddScoped<CitaService>();       // Añadido para Sprint 2

// 4. Inyección de Validadores (FluentValidation)
builder.Services.AddScoped<IValidator<RegistroPacienteRequestDto>, RegistroPacienteValidator>();
builder.Services.AddScoped<IValidator<CrearMedicoRequestDto>, CrearMedicoValidator>();

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

// 5. Agregar Middlewares de Seguridad (El orden es estricto: primero AuthN, luego AuthZ)
app.UseAuthentication(); 
app.UseAuthorization();  

// 6. Mapeo de Endpoints
app.MapAuthEndpoints();
app.MapPacienteEndpoints();
app.MapMedicoEndpoints();
app.MapHorarioEndpoints(); // ¡Línea descomentada que soluciona el 404!
// app.MapCitasEndpoints(); // Descomentar cuando crees CitasEndpoints.cs

app.Run();