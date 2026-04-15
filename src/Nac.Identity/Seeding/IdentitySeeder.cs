using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Identity.Data;
using Nac.Identity.Entities;

namespace Nac.Identity.Seeding;

/// <summary>
/// Seeds default roles for existing tenants at application startup.
/// </summary>
public sealed class IdentitySeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        IServiceProvider serviceProvider,
        ILogger<IdentitySeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Seeds default roles for a specific tenant.
    /// Call this when a new tenant is created (e.g. from a tenant-creation endpoint or event handler).
    /// </summary>
    public async Task SeedDefaultRolesForTenantAsync(string tenantId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NacIdentityDbContext>();

        var hasRoles = await dbContext.TenantRoles
            .AnyAsync(r => r.TenantId == tenantId);

        if (hasRoles)
        {
            _logger.LogDebug("Tenant {TenantId} already has roles; skipping", tenantId);
            return;
        }

        _logger.LogInformation("Seeding default roles for tenant {TenantId}", tenantId);
        await SeedRolesOnlyAsync(dbContext, tenantId);
    }

    private static async Task SeedRolesOnlyAsync(
        NacIdentityDbContext dbContext,
        string tenantId)
    {
        var defaults = DefaultRoles.GetDefaults();

        foreach (var def in defaults)
        {
            var exists = await dbContext.TenantRoles
                .AnyAsync(r => r.TenantId == tenantId && r.Name == def.Name);

            if (exists)
                continue;

            dbContext.TenantRoles.Add(new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = def.Name,
                Permissions = [.. def.Permissions],
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

}
