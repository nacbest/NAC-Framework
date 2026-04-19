namespace Nac.Identity.Jwt;

/// <summary>
/// Configuration options for JWT token generation.
/// Bind from appsettings section, e.g. "Jwt".
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Symmetric secret key used to sign tokens. Must be at least 32 characters.</summary>
    public string SecretKey { get; set; } = default!;

    /// <summary>Token issuer claim value.</summary>
    public string Issuer { get; set; } = "NacFramework";

    /// <summary>Token audience claim value.</summary>
    public string Audience { get; set; } = "NacFramework";

    /// <summary>Number of minutes before the token expires. Defaults to 60.</summary>
    public int ExpirationMinutes { get; set; } = 60;
}
