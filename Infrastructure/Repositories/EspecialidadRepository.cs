using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class EspecialidadRepository
{
    private readonly string _connectionString;

    public EspecialidadRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    public async Task<IEnumerable<EspecialidadDto>> ObtenerTodasAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // Se obtienen todas las especialidades ordenadas alfabéticamente
        var sql = @"
            SELECT 
                id_especialidad AS IdEspecialidad,
                nombre AS Nombre,
                descripcion AS Descripcion
            FROM especialidades
            ORDER BY nombre ASC;";

        return await connection.QueryAsync<EspecialidadDto>(sql);
    }
}