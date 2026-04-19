namespace Nac.Identity.Management.Abstractions;

/// <summary>
/// Configuration options for the Identity Management module.
/// Extend as needed; currently a placeholder for future settings (e.g. invite token TTL).
/// </summary>
public sealed class IdentityManagementOptions
{
    /// <summary>Maximum page size for list endpoints. Default: 100.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Default page size for list endpoints. Default: 20.</summary>
    public int DefaultPageSize { get; set; } = 20;
}
