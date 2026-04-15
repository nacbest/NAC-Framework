using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.Core.Persistence;
using Nac.Domain;
using Nac.Mediator.Abstractions;
using Nac.Persistence.Repository;
using Nac.Persistence.UnitOfWork;

namespace Nac.Persistence.Extensions;

/// <summary>
/// DI registration helpers for the NAC persistence layer.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers a module's <typeparamref name="TContext"/>, its <see cref="INacUnitOfWork"/>,
    /// and the <see cref="UnitOfWorkBehavior{TCommand,TResponse}"/> (once per application).
    /// Call once per module DbContext.
    /// </summary>
    /// <remarks>
    /// In multi-module apps, each module registers its own context. Handlers that need
    /// manual transaction control should inject <see cref="EfUnitOfWork{TContext}"/>
    /// with the specific context type, not the non-generic <see cref="IUnitOfWork"/>.
    /// </remarks>
    public static IServiceCollection AddNacPersistence<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : NacDbContext
    {
        if (configureOptions is not null)
            services.AddDbContext<TContext>(configureOptions);
        else
            services.AddDbContext<TContext>();

        // Forward NacDbContext base type so messaging/inbox can resolve it generically
        services.TryAddScoped<NacDbContext>(sp => sp.GetRequiredService<TContext>());

        // Scoped: one EfUnitOfWork per request, mapped to INacUnitOfWork
        services.AddScoped<EfUnitOfWork<TContext>>();
        services.AddScoped<INacUnitOfWork>(sp => sp.GetRequiredService<EfUnitOfWork<TContext>>());

        // UoW behavior — TryAddEnumerable prevents duplicate registration across modules
        services.TryAddEnumerable(ServiceDescriptor.Transient(
            typeof(ICommandBehavior<,>),
            typeof(UnitOfWorkBehavior<,>)));

        return services;
    }

    /// <summary>
    /// Registers <see cref="IRepository{TEntity}"/> and <see cref="IReadRepository{TEntity}"/>
    /// backed by <typeparamref name="TContext"/>. Scoped lifetime matches the DbContext.
    /// Idempotent — duplicate calls for the same entity type are ignored.
    /// </summary>
    public static IServiceCollection AddNacRepository<TEntity, TContext>(
        this IServiceCollection services)
        where TEntity : class
        where TContext : NacDbContext
    {
        services.TryAddScoped<IRepository<TEntity>>(sp =>
            new EfRepository<TEntity>(sp.GetRequiredService<TContext>()));
        services.TryAddScoped<IReadRepository<TEntity>>(sp =>
            new EfRepository<TEntity>(sp.GetRequiredService<TContext>()));
        return services;
    }

    /// <summary>
    /// Scans <paramref name="assembly"/> for all concrete entity types inheriting
    /// <see cref="Entity{TId}"/> and registers <see cref="IRepository{TEntity}"/>
    /// + <see cref="IReadRepository{TEntity}"/> for each, backed by <typeparamref name="TContext"/>.
    /// Idempotent — duplicate registrations are skipped.
    /// </summary>
    /// <example>
    /// <code>
    /// // Host Program.cs — auto-register all entities from CatalogModule assembly
    /// services.AddNacRepositoriesFromAssembly&lt;CatalogDbContext&gt;(typeof(CatalogModule).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddNacRepositoriesFromAssembly<TContext>(
        this IServiceCollection services,
        Assembly assembly)
        where TContext : NacDbContext
    {
        var entityTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && IsEntityType(t));

        foreach (var entityType in entityTypes)
        {
            var repoInterface = typeof(IRepository<>).MakeGenericType(entityType);
            var readRepoInterface = typeof(IReadRepository<>).MakeGenericType(entityType);

            // Skip if already registered
            if (services.Any(sd => sd.ServiceType == repoInterface))
                continue;

            var efRepoType = typeof(EfRepository<>).MakeGenericType(entityType);

            services.TryAddScoped(repoInterface, sp =>
                Activator.CreateInstance(efRepoType, sp.GetRequiredService<TContext>())!);
            services.TryAddScoped(readRepoInterface, sp =>
                Activator.CreateInstance(efRepoType, sp.GetRequiredService<TContext>())!);
        }

        return services;
    }

    /// <summary>
    /// Checks if <paramref name="type"/> inherits from <see cref="Entity{TId}"/> at any depth.
    /// </summary>
    private static bool IsEntityType(Type type)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Entity<>))
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
