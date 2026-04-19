namespace Nac.Identity.Permissions.Grants;

/// <summary>
/// Data-access surface for <see cref="PermissionGrant"/>. Queries are keyed by
/// <c>(ProviderName, ProviderKey, TenantId?)</c> — the natural cache key shape.
/// </summary>
public interface IPermissionGrantRepository
{
    /// <summary>Lists all permission names granted for the given provider tuple.</summary>
    Task<HashSet<string>> ListGrantsAsync(string providerName, string providerKey,
                                          string? tenantId, CancellationToken ct = default);

    /// <summary>Adds a grant (idempotent — no-op if already present).</summary>
    Task AddGrantAsync(string providerName, string providerKey, string permissionName,
                       string? tenantId, CancellationToken ct = default);

    /// <summary>Removes a grant (no-op if absent).</summary>
    Task RemoveGrantAsync(string providerName, string providerKey, string permissionName,
                          string? tenantId, CancellationToken ct = default);

    /// <summary>Lists all grants for a permission name (introspection/admin).</summary>
    Task<IReadOnlyList<PermissionGrant>> ListGrantsByPermissionAsync(string permissionName,
                                                                    string? tenantId,
                                                                    CancellationToken ct = default);
}
