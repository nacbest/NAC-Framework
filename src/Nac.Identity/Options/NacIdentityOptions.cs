using Nac.Identity.Seeding;

namespace Nac.Identity.Options;

/// <summary>
/// Configuration options for NAC Identity services.
/// </summary>
public sealed class NacIdentityOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "NacIdentity";

    /// <summary>JWT signing key (for HS256). Min 32 chars.</summary>
    public string? SigningKey { get; set; }

    /// <summary>JWT issuer claim.</summary>
    public string Issuer { get; set; } = "nac-identity";

    /// <summary>JWT audience claim.</summary>
    public string Audience { get; set; } = "nac-api";

    /// <summary>Access token lifetime. Default: 15 minutes.</summary>
    public TimeSpan AccessTokenExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Refresh token lifetime. Default: 7 days.</summary>
    public TimeSpan RefreshTokenExpiry { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Whether to use Redis for refresh token storage.</summary>
    public bool UseRedisRefreshTokenStore { get; set; }

    /// <summary>Redis connection string (if UseRedisRefreshTokenStore).</summary>
    public string? RedisConnection { get; set; }

    /// <summary>Default roles to seed for new tenants.</summary>
    public List<DefaultRoleDefinition> DefaultRoles { get; set; } = [];
}
