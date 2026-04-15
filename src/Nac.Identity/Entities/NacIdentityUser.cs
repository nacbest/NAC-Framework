using Microsoft.AspNetCore.Identity;

namespace Nac.Identity.Entities;

/// <summary>
/// NAC Framework base user. Consumers extend: class AppUser : NacIdentityUser { ... }
/// Global user account; permissions scoped per-tenant via TenantMembership.
/// </summary>
public class NacIdentityUser : IdentityUser<Guid>
{
    /// <summary>Display name (separate from UserName for login).</summary>
    public string? DisplayName { get; set; }

    /// <summary>Primary tenant ID (optional; used by TenantAwareUserManager).</summary>
    public string? TenantId { get; set; }

    /// <summary>Account creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Last update timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>ID of the user who created this account.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>User's tenant memberships (navigational).</summary>
    public ICollection<TenantMembership> TenantMemberships { get; set; } = [];

    /// <summary>User's refresh tokens (navigational).</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
