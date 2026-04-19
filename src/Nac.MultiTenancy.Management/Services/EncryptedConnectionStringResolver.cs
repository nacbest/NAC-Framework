using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Factory;

namespace Nac.MultiTenancy.Management.Services;

/// <summary>
/// <see cref="ITenantConnectionStringResolver"/> that decrypts the ciphertext
/// stored on the tenant aggregate via DataProtection. Falls back to the
/// configuration-level <c>ConnectionStrings:Default</c> entry when the tenant
/// has no per-tenant connection string (i.e. <c>Shared</c> isolation).
/// </summary>
/// <remarks>
/// The DataProtection purpose <c>"Nac.MultiTenancy.Management.ConnectionString"</c>
/// must match the purpose used by <c>ITenantManagementService</c> at write time —
/// changing it breaks all stored ciphertext.
/// </remarks>
public sealed class EncryptedConnectionStringResolver : ITenantConnectionStringResolver
{
    /// <summary>DataProtection purpose used for tenant connection-string ciphertext.</summary>
    public const string ProtectorPurpose = "Nac.MultiTenancy.Management.ConnectionString";

    private readonly ITenantStore _store;
    private readonly IDataProtector _protector;
    private readonly string _defaultConnectionString;

    /// <summary>
    /// Initialises a new instance of <see cref="EncryptedConnectionStringResolver"/>.
    /// </summary>
    public EncryptedConnectionStringResolver(
        ITenantStore store,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration)
    {
        _store = store;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _defaultConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Default connection string not configured. " +
                "Add 'ConnectionStrings:Default' to your application settings.");
    }

    /// <inheritdoc />
    public string Resolve(string tenantId)
    {
        // Sync interface (ITenantConnectionStringResolver) — bridge to async store.
        var info = _store.GetByIdAsync(tenantId).GetAwaiter().GetResult();
        if (info?.ConnectionString is { Length: > 0 } cipher)
            return _protector.Unprotect(cipher);
        return _defaultConnectionString;
    }
}
