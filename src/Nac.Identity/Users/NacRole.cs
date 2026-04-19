using Microsoft.AspNetCore.Identity;
using Nac.Core.Primitives;

namespace Nac.Identity.Users;

/// <summary>
/// Application role scoped per tenant. <c>TenantId = null + IsTemplate = true</c> marks
/// a system template that tenants clone at onboarding. Cloned rows have
/// <c>TenantId = {tenant}, IsTemplate = false</c> and are immutable in v3.
/// </summary>
public class NacRole : IdentityRole<Guid>, IAuditableEntity, ISoftDeletable
{
    /// <summary>Tenant slug owning this role. Null indicates a system template.</summary>
    public string? TenantId { get; set; }

    /// <summary>True when this role is a system template eligible for cloning.</summary>
    public bool IsTemplate { get; set; }

    /// <summary>Optional description of the role's purpose.</summary>
    public string? Description { get; set; }

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
    protected NacRole() { }

    /// <summary>
    /// Creates a new role. <paramref name="tenantId"/> <c>null</c> = system template.
    /// </summary>
    public NacRole(string roleName, string? tenantId = null, bool isTemplate = false, string? description = null)
        : base(roleName)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        IsTemplate = isTemplate;
        Description = description;
    }
}
