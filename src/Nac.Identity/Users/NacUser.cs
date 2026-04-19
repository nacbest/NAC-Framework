using Microsoft.AspNetCore.Identity;
using Nac.Core.Domain;
using Nac.Core.Primitives;

namespace Nac.Identity.Users;

/// <summary>
/// Application user that extends <see cref="IdentityUser{TKey}"/> with multi-tenancy,
/// auditing, and soft-delete support for the NAC Framework.
/// </summary>
public class NacUser : IdentityUser<Guid>, ITenantEntity, IAuditableEntity, ISoftDeletable
{
    /// <summary>Display name of the user.</summary>
    public string? FullName { get; set; }

    /// <inheritdoc cref="ITenantEntity.TenantId"/>
    public string TenantId { get; set; } = default!;

    /// <summary>Indicates whether the user account is active.</summary>
    public bool IsActive { get; set; } = true;

    // ── IAuditableEntity ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Required by EF Core.</summary>
    protected NacUser() { }

    /// <summary>
    /// Creates a new <see cref="NacUser"/> with the given email and tenant.
    /// <see cref="IdentityUser{TKey}.UserName"/> is set to <paramref name="email"/>.
    /// </summary>
    public NacUser(string email, string tenantId)
    {
        Id = Guid.NewGuid();
        Email = email;
        UserName = email;
        TenantId = tenantId;
    }
}
