using Api.Endpoints;
using Application.Services;         // Inyección de Servicios modularizados
using Application.Validators;       // Validadores FluentValidation
using Domain;
using Domain.DTOs;                 // DTOs agrupados (PacienteDTOs, MedicoDTOs, AuthDTOs, CitaDTOs)
using Domain.Entities;
using FluentValidation;
using Infrastructure;              // Extensión AddDatabaseConfiguration
using Infrastructure.Repositories; // Repositorios modularizados
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

// Configuración de la base de datos (PostgreSQL/Dapper)
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

// Habilitar Autorización basada en roles
builder.Services.AddAuthorization();

// 2. Inyección de Repositorios (Infrastructure.Repositories)
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<PacienteRepository>();
builder.Services.AddScoped<MedicoRepository>();
builder.Services.AddScoped<HorarioRepository>();
builder.Services.AddScoped<CitaRepository>();

// 3. Inyección de Servicios (Application.Services)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PacienteService>();
builder.Services.AddScoped<MedicoService>();
builder.Services.AddScoped<HorarioService>();
builder.Services.AddScoped<CitaService>();

// 4. Inyección de Validadores (FluentValidation)
builder.Services.AddScoped<IValidator<RegistroPacienteRequestDto>, RegistroPacienteValidator>();
builder.Services.AddScoped<IValidator<CrearMedicoRequestDto>, CrearMedicoValidator>();

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

// 5. Middlewares de Seguridad (Orden estricto)
app.UseAuthentication(); 
app.UseAuthorization();  

// 6. Mapeo de Endpoints
app.MapAuthEndpoints();
app.MapPacienteEndpoints();
app.MapMedicoEndpoints();
app.MapHorarioEndpoints();
app.MapCitasEndpoints();

app.Run();