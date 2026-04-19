using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Context;
using Nac.MultiTenancy.EfCore;
using Nac.MultiTenancy.Factory;
using Nac.MultiTenancy.Resolution;

namespace Nac.MultiTenancy.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that register NAC multi-tenancy services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core multi-tenancy services: tenant context, resolution strategies,
    /// the tenant entity EF Core interceptor, and (optionally) per-tenant database support.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="MultiTenancyOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddNacMultiTenancy(
        this IServiceCollection services,
        Action<MultiTenancyOptions>? configure = null)
    {
        var options = new MultiTenancyOptions();
        configure?.Invoke(options);

        // Singleton: AsyncLocal inside TenantContext provides per-async-flow scoping.
        services.AddSingleton<ITenantContext, TenantContext>();

        // Register each resolution strategy as a named Scoped implementation of the interface.
        // The middleware resolves IEnumerable<ITenantResolutionStrategy> and tries them in order.
        foreach (var strategyType in options.Strategies)
            services.AddScoped(typeof(ITenantResolutionStrategy), strategyType);

        // Optional: per-tenant database connection string resolution.
        if (options.EnablePerTenantDatabase)
            services.AddScoped<ITenantConnectionStringResolver, TenantConnectionStringResolver>();

        // Scoped interceptor: auto-stamps TenantId on new ITenantEntity rows.
        services.AddScoped<SaveChangesInterceptor, TenantEntityInterceptor>();

        return services;
    }
}
