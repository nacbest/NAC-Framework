using Microsoft.Extensions.Configuration;
using Nac.MultiTenancy.Abstractions;

namespace Nac.MultiTenancy.Factory;

/// <summary>
/// Default <see cref="ITenantConnectionStringResolver"/> that looks up the
/// per-tenant connection string from <see cref="ITenantStore"/>.
/// Falls back to the <c>Default</c> connection string from
/// <see cref="IConfiguration"/> when the tenant has no dedicated database.
/// </summary>
internal sealed class TenantConnectionStringResolver : ITenantConnectionStringResolver
{
    private readonly ITenantStore _tenantStore;
    private readonly string _defaultConnectionString;

    /// <summary>
    /// Initialises a new instance of <see cref="TenantConnectionStringResolver"/>.
    /// </summary>
    /// <param name="tenantStore">Source of tenant metadata including per-tenant connection strings.</param>
    /// <param name="configuration">Application configuration used to read the fallback connection string.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>Default</c> connection string is absent from configuration.
    /// </exception>
    public TenantConnectionStringResolver(ITenantStore tenantStore, IConfiguration configuration)
    {
        _tenantStore = tenantStore;
        _defaultConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Default connection string not configured. " +
                "Add 'ConnectionStrings:Default' to your application settings.");
    }

    /// <inheritdoc />
    public string Resolve(string tenantId)
    {
        var tenant = _tenantStore.GetByIdAsync(tenantId).GetAwaiter().GetResult();
        return tenant?.ConnectionString ?? _defaultConnectionString;
    }
}
