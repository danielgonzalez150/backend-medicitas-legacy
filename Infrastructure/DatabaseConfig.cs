using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDbConnection>(sp => 
            new NpgsqlConnection(configuration.GetConnectionString("SupabaseDB")));
        return services;
    }
}