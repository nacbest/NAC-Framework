namespace Nac.MultiTenancy;

/// <summary>
/// Provides the tenant context for the current request scope.
/// When multi-tenancy is disabled, <see cref="IsMultiTenant"/> returns false
/// and <see cref="Current"/> returns null — zero overhead.
/// </summary>
public interface ITenantContext
{
    /// <summary>Whether multi-tenancy is enabled for this application.</summary>
    bool IsMultiTenant { get; }

    /// <summary>The resolved tenant for the current request. Null when multi-tenancy is disabled or tenant not yet resolved.</summary>
    TenantInfo? Current { get; }

    /// <summary>The tenant ID shortcut. Null when no tenant is resolved.</summary>
    string? TenantId => Current?.Id;
}
