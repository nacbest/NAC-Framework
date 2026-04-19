using Microsoft.AspNetCore.Identity;
using Nac.Identity.Jwt;

namespace Nac.Identity.Extensions;

/// <summary>
/// Top-level options for configuring NAC Identity, bundling JWT settings
/// and ASP.NET Core Identity options into a single fluent setup object.
/// </summary>
public sealed class NacIdentityOptions
{
    /// <summary>JWT token generation and validation settings.</summary>
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>
    /// Optional delegate to further configure <see cref="IdentityOptions"/>
    /// (password policy, lockout, user validation, etc.).
    /// </summary>
    public Action<IdentityOptions>? ConfigureIdentity { get; set; }
}
