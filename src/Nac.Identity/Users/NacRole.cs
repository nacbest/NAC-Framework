using Microsoft.AspNetCore.Identity;

namespace Nac.Identity.Users;

/// <summary>
/// Application role that extends <see cref="IdentityRole{TKey}"/> with an optional
/// human-readable description.
/// </summary>
public class NacRole : IdentityRole<Guid>
{
    /// <summary>Optional description of the role's purpose.</summary>
    public string? Description { get; set; }

    /// <summary>Required by EF Core.</summary>
    protected NacRole() { }

    /// <summary>
    /// Creates a new <see cref="NacRole"/> with the given name and optional description.
    /// </summary>
    public NacRole(string roleName, string? description = null) : base(roleName)
    {
        Id = Guid.NewGuid();
        Description = description;
    }
}
