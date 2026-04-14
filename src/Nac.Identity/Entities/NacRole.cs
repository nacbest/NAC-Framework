using Microsoft.AspNetCore.Identity;

namespace Nac.Identity.Entities;

/// <summary>
/// System-wide role (rarely used). Prefer TenantRole for per-tenant permissions.
/// </summary>
public sealed class NacRole : IdentityRole<Guid>
{
    public NacRole() { }
    public NacRole(string roleName) : base(roleName) { }
}
