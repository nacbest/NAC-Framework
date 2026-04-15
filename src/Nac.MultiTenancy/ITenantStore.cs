using Nac.Core.MultiTenancy;

namespace Nac.MultiTenancy;

/// <summary>
/// Retrieves <see cref="TenantInfo"/> by tenant identifier.
/// Implementations may read from a database (host DB), configuration file,
/// or in-memory dictionary. The middleware calls this after a resolver
/// returns a tenant ID string.
/// </summary>
public interface ITenantStore
{
    /// <summary>Looks up full tenant metadata by identifier.</summary>
    Task<TenantInfo?> GetByIdAsync(string tenantId, CancellationToken ct = default);
}
