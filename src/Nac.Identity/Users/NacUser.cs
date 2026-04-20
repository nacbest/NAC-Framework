using Microsoft.AspNetCore.Identity;
using Nac.Core.Primitives;

namespace Nac.Identity.Users;

/// <summary>
/// Application user that extends <see cref="IdentityUser{TKey}"/> with auditing and
/// soft-delete support. Pattern A (portable user): users are global — tenant access
/// is granted via <c>UserTenantMembership</c> rows.
/// </summary>
public class NacUser : IdentityUser<Guid>, IAuditableEntity, ISoftDeletable
{
    /// <summary>Display name of the user.</summary>
    public string? FullName { get; set; }

    /// <summary>
    /// When <c>true</c>, this user is a host (platform) account. Combined with the
    /// <c>Host.AccessAllTenants</c> permission it bypasses tenant query filters.
    /// Never settable via API — seeded or set via migration only.
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>Indicates whether the user account is active.</summary>
    public bool IsActive { get; set; } = true;

    // ── IAuditableEntity ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <inheritdoc/>
    public string? ImpersonatorId { get; set; }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Required by EF Core.</summary>
    protected NacUser() { }

    /// <summary>
    /// Creates a new <see cref="NacUser"/> with the given email. <see cref="IdentityUser{TKey}.UserName"/>
    /// is set to <paramref name="email"/>.
    /// </summary>
    public NacUser(string email, string? fullName = null)
    {
        Id = Guid.NewGuid();
        Email = email;
        UserName = email;
        FullName = fullName;
    }
}
