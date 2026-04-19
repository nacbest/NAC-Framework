namespace Nac.Core.Abstractions.Permissions;

/// <summary>
/// Evaluates permission grants. Implementations resolve grants via cache→DB;
/// tree ancestor matches are supported.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>Checks the permission for the current authenticated user.</summary>
    Task<bool> IsGrantedAsync(string permissionName, CancellationToken ct = default);

    /// <summary>Cross-user check — resolves grants for an arbitrary user in a given tenant.</summary>
    Task<bool> IsGrantedAsync(Guid userId, string permissionName, string? tenantId = null,
                              CancellationToken ct = default);

    /// <summary>
    /// Resource-aware overload. v3 stub: falls back to name-only check and logs a warning.
    /// </summary>
    Task<bool> IsGrantedAsync(string permissionName, string resourceType, string resourceId,
                              CancellationToken ct = default);

    /// <summary>Batch evaluation against the current user.</summary>
    Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] permissionNames,
                                                      CancellationToken ct = default);
}
