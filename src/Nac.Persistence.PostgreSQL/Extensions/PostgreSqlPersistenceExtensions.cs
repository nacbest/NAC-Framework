using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Nac.Persistence.Extensions;

namespace Nac.Persistence.PostgreSQL.Extensions;

/// <summary>
/// Convenience extensions that combine <see cref="PersistenceServiceCollectionExtensions.AddNacPersistence{TContext}"/>
/// with the Npgsql provider in a single call.
/// </summary>
public static class PostgreSqlPersistenceExtensions
{
    /// <summary>
    /// Registers a module's <typeparamref name="TContext"/> with PostgreSQL as the database provider.
    /// Combines AddNacPersistence + UseNpgsql in one call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="configureNpgsql">Optional Npgsql-specific configuration.</param>
    public static IServiceCollection AddNacPostgreSQL<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
        where TContext : NacDbContext
    {
        return services.AddNacPersistence<TContext>(options =>
            options.UseNpgsql(connectionString, configureNpgsql ?? (_ => { })));
    }
}
