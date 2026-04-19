using Nac.Identity.Users;

namespace Nac.Identity.Roles;

/// <summary>
/// Role CRUD scoped per tenant and permission grant mutations. Phase 04 fleshes out
/// the template/clone semantics; this shell carries the contract and the grant/revoke
/// cache-invalidation path.
/// </summary>
public interface IRoleService
{
    Task<NacRole> CreateAsync(string tenantId, string name, string? description = null,
                              CancellationToken ct = default);

    Task<IReadOnlyList<NacRole>> ListForTenantAsync(string tenantId, CancellationToken ct = default);

    Task DeleteAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>Clones a system template role into a tenant (Phase 04 impl).</summary>
    Task<NacRole> CloneFromTemplateAsync(string tenantId, Guid templateRoleId, string? newName = null,
                                         CancellationToken ct = default);

    /// <summary>Grants a permission to a role; invalidates the role cache key.</summary>
    Task GrantPermissionAsync(Guid roleId, string permissionName, string tenantId,
                              CancellationToken ct = default);

    /// <summary>Revokes a permission from a role; invalidates the role cache key.</summary>
    Task RevokePermissionAsync(Guid roleId, string permissionName, string tenantId,
                               CancellationToken ct = default);

    /// <summary>Returns the permission names currently granted to <paramref name="roleId"/>.</summary>
    Task<IReadOnlyList<string>> ListGrantsAsync(Guid roleId, string? tenantId,
                                                CancellationToken ct = default);

    /// <summary>Returns all system template roles (<c>IsTemplate=true</c>).</summary>
    Task<IReadOnlyList<NacRole>> ListTemplatesAsync(CancellationToken ct = default);
}
