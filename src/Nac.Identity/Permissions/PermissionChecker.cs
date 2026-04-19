using Microsoft.AspNetCore.Http;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Services;

namespace Nac.Identity.Permissions;

/// <summary>
/// Evaluates permission grants for the current HTTP request user.
///
/// Grant resolution order:
///   1. Explicit <c>permission</c> claim on the principal matching the requested name.
///   2. Implicit grant via an ancestor permission claim — if the principal holds a
///      parent permission, all descendant permissions are considered granted.
///   3. Fallback: denied (IsEnabled is informational; it does not auto-grant).
/// </summary>
internal sealed class PermissionChecker(
    IHttpContextAccessor httpContextAccessor,
    PermissionDefinitionManager definitionManager) : IPermissionChecker
{
    /// <inheritdoc/>
    public Task<bool> IsGrantedAsync(string permissionName)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Task.FromResult(false);

        return Task.FromResult(CheckPermission(user, permissionName));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Not yet implemented — requires loading persisted grants by userId.
    /// Currently only claims-based check for the current HTTP user is supported.
    /// </remarks>
    public Task<bool> IsGrantedAsync(Guid userId, string permissionName) =>
        throw new NotSupportedException(
            "Cross-user permission check not yet implemented. " +
            "Use the parameterless overload for the current HTTP user.");

    /// <inheritdoc/>
    public Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] permissionNames)
    {
        var result = new MultiplePermissionGrantResult();
        var user = httpContextAccessor.HttpContext?.User;
        bool authenticated = user?.Identity?.IsAuthenticated == true;

        foreach (var name in permissionNames)
            result.SetResult(name, authenticated && CheckPermission(user!, name));

        return Task.FromResult(result);
    }

    // ── internal logic ───────────────────────────────────────────────────────

    private bool CheckPermission(System.Security.Claims.ClaimsPrincipal user, string permissionName)
    {
        // 1. Explicit claim match.
        if (user.HasClaim(NacIdentityClaims.Permission, permissionName))
            return true;

        // 2. Hierarchical: granted if any held permission claim is an ancestor
        //    of the requested permission in the definition tree.
        var definition = definitionManager.GetOrNull(permissionName);
        if (definition is null)
            return false;

        var heldPermissionClaims = user.FindAll(NacIdentityClaims.Permission);
        foreach (var claim in heldPermissionClaims)
        {
            var heldDef = definitionManager.GetOrNull(claim.Value);
            if (heldDef is not null && IsDescendantOf(heldDef, permissionName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true when <paramref name="targetName"/> is reachable by walking
    /// the children of <paramref name="ancestor"/> recursively.
    /// </summary>
    private static bool IsDescendantOf(PermissionDefinition ancestor, string targetName)
    {
        foreach (var child in ancestor.Children)
        {
            if (string.Equals(child.Name, targetName, StringComparison.Ordinal))
                return true;

            if (IsDescendantOf(child, targetName))
                return true;
        }

        return false;
    }
}
