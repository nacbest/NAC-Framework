using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions;
using Nac.Core.Domain;
using Nac.Persistence.Context;
using Nac.Persistence.Interceptors;
using Nac.Persistence.Outbox;
using Nac.Persistence.Repository;

namespace Nac.Persistence.Extensions;

/// <summary>
/// Extension methods for registering NAC Framework persistence services into an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the concrete <typeparamref name="TContext"/> as the EF Core DbContext,
    /// wires up <see cref="IUnitOfWork"/>, registers open-generic
    /// <see cref="IRepository{T}"/> / <see cref="IReadRepository{T}"/> implementations,
    /// and optionally enables interceptors and the transactional outbox based on
    /// <see cref="NacPersistenceOptions"/>.
    /// </summary>
    /// <typeparam name="TContext">
    /// The application-specific DbContext that inherits from <see cref="NacDbContext"/>.
    /// </typeparam>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configure">
    /// Optional callback to configure <see cref="NacPersistenceOptions"/>, e.g. to supply
    /// the database provider via <see cref="NacPersistenceOptions.UseDbContext"/> or to
    /// enable interceptors via the <c>Enable*</c> methods.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddNacPersistence<TContext>(
        this IServiceCollection services,
        Action<NacPersistenceOptions>? configure = null)
        where TContext : NacDbContext
    {
        var options = new NacPersistenceOptions();
        configure?.Invoke(options);

        // Register enabled interceptors as scoped services so they can resolve
        // ICurrentUser / IDateTimeProvider / IServiceProvider from the current scope.
        RegisterInterceptors(services, options);

        // Register the concrete context. Interceptors are resolved from the service provider
        // so they participate in the same DI scope as the DbContext.
        services.AddDbContext<TContext>((sp, builder) =>
        {
            options.ConfigureDbContext?.Invoke(builder);

            var interceptors = sp.GetServices<SaveChangesInterceptor>().ToArray();
            if (interceptors.Length > 0)
                builder.AddInterceptors(interceptors);
        });

        // Expose NacDbContext as the base type so Repository<T> can be resolved
        // without depending on the concrete context type.
        services.AddScoped<NacDbContext>(sp => sp.GetRequiredService<TContext>());

        // IUnitOfWork delegates to the same TContext instance within the scope.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());

        // Open-generic repository registrations — resolved per entity type at runtime.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));

        // Outbox background worker (hosted service, singleton lifetime via IHostedService).
        if (options.OutboxEnabled)
            services.AddHostedService<OutboxWorker>();

        return services;
    }

    /// <summary>
    /// Registers each enabled interceptor as <see cref="SaveChangesInterceptor"/> in the
    /// DI container. The DbContext factory resolves all registrations via
    /// <c>GetServices&lt;SaveChangesInterceptor&gt;()</c> and passes them to
    /// <c>DbContextOptionsBuilder.AddInterceptors()</c>.
    /// </summary>
    private static void RegisterInterceptors(IServiceCollection services, NacPersistenceOptions options)
    {
        if (options.AuditEnabled)
            services.AddScoped<SaveChangesInterceptor, AuditableEntityInterceptor>();

        if (options.SoftDeleteEnabled)
            services.AddScoped<SaveChangesInterceptor, SoftDeleteInterceptor>();

        if (options.DomainEventEnabled)
            services.AddScoped<SaveChangesInterceptor, DomainEventInterceptor>();

        if (options.OutboxEnabled)
            services.AddScoped<SaveChangesInterceptor, OutboxInterceptor>();
    }
}
