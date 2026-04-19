using Microsoft.EntityFrameworkCore;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Core.Domain;
using Nac.Identity.Permissions.Host;

namespace Nac.Identity.Persistence;

/// <summary>
/// Extension that bypasses the global tenant query filter for host users.
/// Callers must await; enforces both IsHost flag and Host.AccessAllTenants permission.
/// </summary>
public static class HostQueryExtensions
{
    /// <summary>
    /// Returns <paramref name="query"/> with <c>IgnoreQueryFilters()</c> applied.
    /// Throws <see cref="ForbiddenAccessException"/> (→ HTTP 403) unless the current user
    /// is a host AND holds <c>Host.AccessAllTenants</c>.
    /// </summary>
    public static async Task<IQueryable<T>> AsHostQueryAsync<T>(
        this IQueryable<T> query,
        ICurrentUser user,
        IPermissionChecker permissionChecker,
        CancellationToken ct = default) where T : class
    {
        if (!user.IsHost)
            throw new ForbiddenAccessException("Host user required.");

        if (!await permissionChecker.IsGrantedAsync(HostPermissions.AccessAllTenants, ct))
            throw new ForbiddenAccessException($"{HostPermissions.AccessAllTenants} permission required.");

        return query.IgnoreQueryFilters();
    }
}
