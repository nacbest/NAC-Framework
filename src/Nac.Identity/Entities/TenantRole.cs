namespace Nac.Identity.Entities;

/// <summary>
/// Role definition scoped to a specific tenant.
/// Contains permission strings (e.g., "catalog.products.create").
/// </summary>
public sealed class TenantRole
{
    public Guid Id { get; set; }

    /// <summary>Tenant this role belongs to.</summary>
    public required string TenantId { get; set; }

    /// <summary>Role name (e.g., "Owner", "Admin", "Member").</summary>
    public required string Name { get; set; }

    /// <summary>Permission strings granted by this role.</summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>Role creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Memberships using this role (navigational).</summary>
    public ICollection<TenantMembership> Memberships { get; set; } = [];
}
