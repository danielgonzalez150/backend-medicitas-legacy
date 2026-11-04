using Application;
using Infrastructure;
using Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddDatabaseConfiguration(builder.Configuration);

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<PacienteRepository>();
builder.Services.AddScoped<MedicoRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PacienteService>();

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

app.MapAuthEndpoints();
app.MapPacienteEndpoints();
app.MapMedicoEndpoints();

app.Run();