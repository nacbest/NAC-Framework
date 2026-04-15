using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nac.Core.MultiTenancy;
using Nac.MultiTenancy.Resolvers;

namespace Nac.MultiTenancy.Extensions;

/// <summary>
/// DI and pipeline registration for NAC multi-tenancy.
/// </summary>
public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registers multi-tenancy services with the given resolvers and tenant store.
    /// Resolvers are tried in registration order (first non-null result wins).
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddNacMultiTenancy(mt =>
    /// {
    ///     mt.AddHeaderResolver();
    ///     mt.AddClaimResolver();
    ///     mt.UseInMemoryStore([new TenantInfo("acme", "Acme Corp")]);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddNacMultiTenancy(
        this IServiceCollection services,
        Action<MultiTenancyBuilder> configure)
    {
        services.TryAddScoped<ITenantContext, TenantContext>();

        var builder = new MultiTenancyBuilder(services);
        configure(builder);

        return services;
    }

    /// <summary>
    /// Inserts the tenant resolution middleware into the pipeline.
    /// Must be called after authentication middleware (so claims are available)
    /// and before module endpoints.
    /// </summary>
    public static IApplicationBuilder UseNacMultiTenancy(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }
}

/// <summary>
/// Fluent builder for configuring multi-tenancy resolvers and store.
/// </summary>
public sealed class MultiTenancyBuilder
{
    private readonly IServiceCollection _services;

    internal MultiTenancyBuilder(IServiceCollection services) => _services = services;

    /// <summary>Adds header-based resolver (default header: <c>X-Tenant-ID</c>).</summary>
    public MultiTenancyBuilder AddHeaderResolver(string headerName = "X-Tenant-ID")
    {
        _services.AddSingleton<ITenantResolver>(new HeaderTenantResolver(headerName));
        return this;
    }

    /// <summary>Adds JWT claim-based resolver (default claim: <c>tenant_id</c>).</summary>
    public MultiTenancyBuilder AddClaimResolver(string claimType = "tenant_id")
    {
        _services.AddSingleton<ITenantResolver>(new ClaimTenantResolver(claimType));
        return this;
    }

    /// <summary>Adds subdomain-based resolver (<c>acme.myapp.com</c> → <c>acme</c>).</summary>
    public MultiTenancyBuilder AddSubdomainResolver()
    {
        _services.AddSingleton<ITenantResolver>(new SubdomainTenantResolver());
        return this;
    }

    /// <summary>Adds query string-based resolver (default param: <c>tenant</c>).</summary>
    public MultiTenancyBuilder AddQueryStringResolver(string parameterName = "tenant")
    {
        _services.AddSingleton<ITenantResolver>(new QueryStringTenantResolver(parameterName));
        return this;
    }

    /// <summary>Adds a custom resolver.</summary>
    public MultiTenancyBuilder AddResolver<TResolver>() where TResolver : class, ITenantResolver
    {
        _services.AddSingleton<ITenantResolver, TResolver>();
        return this;
    }

    /// <summary>Uses an in-memory tenant store. Suitable for development/testing.</summary>
    public MultiTenancyBuilder UseInMemoryStore(IEnumerable<TenantInfo> tenants)
    {
        _services.TryAddSingleton<ITenantStore>(new InMemoryTenantStore(tenants));
        return this;
    }

    /// <summary>Uses a custom tenant store (e.g., database-backed).</summary>
    public MultiTenancyBuilder UseStore<TStore>() where TStore : class, ITenantStore
    {
        _services.TryAddScoped<ITenantStore, TStore>();
        return this;
    }
}
