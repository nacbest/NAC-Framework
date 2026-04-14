using Microsoft.AspNetCore.Identity;

namespace Nac.Identity.Entities;

/// <summary>
/// NAC Framework user extending ASP.NET Core Identity.
/// Global user account; permissions scoped per-tenant via TenantMembership.
/// </summary>
public sealed class NacUser : IdentityUser<Guid>
{
    /// <summary>Display name (separate from UserName for login).</summary>
    public string? DisplayName { get; set; }

    /// <summary>User's tenant memberships (navigational).</summary>
    public ICollection<TenantMembership> TenantMemberships { get; set; } = [];

    /// <summary>User's refresh tokens (navigational).</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>Account creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
