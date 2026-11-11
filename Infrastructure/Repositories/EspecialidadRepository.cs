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

        // Se corrigió el nombre de la columna a 'nombre_especialidad' y se eliminó 'descripcion' porque no existe en la BD.
        var sql = @"
            SELECT 
                id_especialidad AS IdEspecialidad,
                nombre_especialidad AS Nombre
            FROM especialidades
            ORDER BY nombre_especialidad ASC;";

        return await connection.QueryAsync<EspecialidadDto>(sql);
    }
}