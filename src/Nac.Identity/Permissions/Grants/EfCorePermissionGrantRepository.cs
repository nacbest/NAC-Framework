using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;

namespace Nac.Identity.Permissions.Grants;

/// <summary>
/// EF Core implementation of <see cref="IPermissionGrantRepository"/>. Uses the active
/// <see cref="NacIdentityDbContext"/> from DI. All operations are idempotent.
/// </summary>
internal sealed class EfCorePermissionGrantRepository(NacIdentityDbContext db) : IPermissionGrantRepository
{
    public async Task<HashSet<string>> ListGrantsAsync(string providerName, string providerKey,
                                                      string? tenantId, CancellationToken ct = default)
    {
        var names = await db.PermissionGrants
            .AsNoTracking()
            .Where(g => g.ProviderName == providerName
                     && g.ProviderKey == providerKey
                     && g.TenantId == tenantId)
            .Select(g => g.PermissionName)
            .ToListAsync(ct);

        return new HashSet<string>(names, StringComparer.Ordinal);
    }

    public async Task AddGrantAsync(string providerName, string providerKey, string permissionName,
                                    string? tenantId, CancellationToken ct = default)
    {
        var exists = await db.PermissionGrants.AnyAsync(
            g => g.ProviderName == providerName
              && g.ProviderKey == providerKey
              && g.PermissionName == permissionName
              && g.TenantId == tenantId, ct);
        if (exists) return;

        db.PermissionGrants.Add(new PermissionGrant(providerName, providerKey, permissionName, tenantId));
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveGrantAsync(string providerName, string providerKey, string permissionName,
                                       string? tenantId, CancellationToken ct = default)
    {
        var grant = await db.PermissionGrants.FirstOrDefaultAsync(
            g => g.ProviderName == providerName
              && g.ProviderKey == providerKey
              && g.PermissionName == permissionName
              && g.TenantId == tenantId, ct);
        if (grant is null) return;

        db.PermissionGrants.Remove(grant);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PermissionGrant>> ListGrantsByPermissionAsync(string permissionName,
                                                                                  string? tenantId,
                                                                                  CancellationToken ct = default)
    {
        return await db.PermissionGrants
            .AsNoTracking()
            .Where(g => g.PermissionName == permissionName && g.TenantId == tenantId)
            .ToListAsync(ct);
    }
}
