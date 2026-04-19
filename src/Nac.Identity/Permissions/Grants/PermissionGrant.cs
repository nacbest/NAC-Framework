namespace Nac.Identity.Permissions.Grants;

/// <summary>
/// Single source of truth for all permission assignments (ABP-style). Extensible via
/// <see cref="ProviderName"/>: <c>"U"</c> = direct user grant, <c>"R"</c> = role grant.
/// Revoke = physical delete (no soft-delete). Audited via <c>CreatedAt</c>.
/// </summary>
public sealed class PermissionGrant
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Provider type code — see <see cref="PermissionProviderNames"/>.</summary>
    public string ProviderName { get; private set; } = default!;

    /// <summary>Stringified id of the target (user id or role id).</summary>
    public string ProviderKey { get; private set; } = default!;

    /// <summary>Permission name (matches <c>PermissionDefinition</c>).</summary>
    public string PermissionName { get; private set; } = default!;

    /// <summary>
    /// Tenant scope. Null = grant applies to system-template roles or global user
    /// grants (e.g. host permissions).
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>UTC timestamp when the grant was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Required by EF Core.</summary>
    private PermissionGrant() { }

    public PermissionGrant(string providerName, string providerKey,
                           string permissionName, string? tenantId)
    {
        Id = Guid.NewGuid();
        ProviderName = providerName;
        ProviderKey = providerKey;
        PermissionName = permissionName;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }
}
