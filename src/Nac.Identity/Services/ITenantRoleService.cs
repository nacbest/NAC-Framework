using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// Service for managing tenant roles and user membership.
/// </summary>
public interface ITenantRoleService
{
    /// <summary>
    /// Initializes default roles for a new tenant.
    /// Called when a tenant is created.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ownerUserId">The user who will be assigned Owner role.</param>
    Task InitializeTenantAsync(string tenantId, Guid ownerUserId);

    /// <summary>
    /// Gets all roles for a tenant.
    /// </summary>
    Task<IReadOnlyList<TenantRole>> GetRolesAsync(string tenantId);

    /// <summary>
    /// Gets a specific role by name within a tenant.
    /// </summary>
    Task<TenantRole?> GetRoleByNameAsync(string tenantId, string roleName);

    /// <summary>
    /// Creates a custom role for a tenant.
    /// </summary>
    Task<TenantRole> CreateRoleAsync(
        string tenantId,
        string name,
        IEnumerable<string> permissions);

    /// <summary>
    /// Updates a role's permissions.
    /// </summary>
    Task UpdateRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions);

    /// <summary>
    /// Deletes a role (fails if users assigned).
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid roleId);

    /// <summary>
    /// Assigns a user to a tenant with a specific role.
    /// </summary>
    Task<TenantMembership> AssignUserToTenantAsync(
        Guid userId,
        string tenantId,
        Guid roleId,
        bool isOwner = false);

    /// <summary>
    /// Changes a user's role within a tenant.
    /// </summary>
    Task ChangeUserRoleAsync(
        Guid userId,
        string tenantId,
        Guid newRoleId);

    /// <summary>
    /// Removes a user from a tenant.
    /// </summary>
    Task RemoveUserFromTenantAsync(Guid userId, string tenantId);

    /// <summary>
    /// Gets a user's membership in a tenant.
    /// </summary>
    Task<TenantMembership?> GetMembershipAsync(Guid userId, string tenantId);
}
