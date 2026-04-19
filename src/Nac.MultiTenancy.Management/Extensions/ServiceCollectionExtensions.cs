using FluentValidation;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Factory;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Authorization;
using Nac.MultiTenancy.Management.Persistence;
using Nac.MultiTenancy.Management.Services;
using Nac.Persistence.Extensions;

namespace Nac.MultiTenancy.Management.Extensions;

/// <summary>
/// DI registration entry point for the tenant management module. Consumer must
/// have already called <c>AddNacMultiTenancy(...)</c>; this extension overrides
/// the default in-memory <see cref="ITenantStore"/> with the EF-backed
/// implementation and wires the encrypted connection-string resolver.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <c>TenantManagementDbContext</c>, the management service, validators,
    /// controllers, authz policy, and overrides the default <see cref="ITenantStore"/>
    /// + <see cref="ITenantConnectionStringResolver"/> registrations.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configure">Mandatory configurator — must call <c>UseDbContext</c>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddNacTenantManagement(
        this IServiceCollection services,
        Action<TenantManagementOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var opts = new TenantManagementOptions();
        configure(opts);
        if (opts.DbContextConfigure is null)
            throw new InvalidOperationException(
                "TenantManagementOptions.UseDbContext(...) must be called.");

        services.AddSingleton<IOptions<TenantManagementOptions>>(Options.Create(opts));

        // Persistence: registers DbContext + outbox + audit + soft-delete + domain-event
        // interceptors. The outbox interceptor will pick up our IIntegrationEvent
        // tenant events automatically.
        services.AddNacPersistence<TenantManagementDbContext>(p => p
            .UseDbContext(opts.DbContextConfigure)
            .EnableAuditInterceptor()
            .EnableSoftDeleteInterceptor()
            .EnableDomainEventInterceptor()
            .EnableOutbox());

        // Override the default in-memory tenant store + plain resolver from Nac.MultiTenancy.
        services.RemoveAll<ITenantStore>();
        services.AddScoped<ITenantStore, EfCoreTenantStore>();

        services.RemoveAll<ITenantConnectionStringResolver>();
        services.AddScoped<ITenantConnectionStringResolver, EncryptedConnectionStringResolver>();

        services.AddMemoryCache();

        // DataProtection — idempotent.
        services.AddDataProtection();

        services.AddScoped<ITenantManagementService, TenantManagementService>();
        services.AddScoped<ITenantCacheInvalidator, TenantCacheInvalidator>();
        services.AddScoped<HostAdminOnlyFilter>();

        services.AddValidatorsFromAssemblyContaining<NacTenantManagementModule>(
            includeInternalTypes: true);

        services.AddAuthorization(authz =>
        {
            authz.AddPolicy(opts.PermissionName, p =>
                p.RequireAuthenticatedUser().RequireClaim("permission", opts.PermissionName));
        });

        services.AddControllers().ConfigureApplicationPartManager(apm =>
        {
            var assembly = typeof(NacTenantManagementModule).Assembly;
            if (!apm.ApplicationParts.OfType<AssemblyPart>()
                    .Any(p => p.Assembly == assembly))
            {
                apm.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        });

        return services;
    }
}
