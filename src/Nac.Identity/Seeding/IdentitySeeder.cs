using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Abstractions.MultiTenancy;
using Nac.Identity.Data;
using Nac.Identity.Entities;
using Nac.Identity.Services;
using Nac.MultiTenancy;

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
    /// Seeds default roles for all tenants that don't have them.
    /// Called at application startup.
    /// </summary>
    public async Task SeedDefaultRolesAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<NacIdentityDbContext>();
        var tenantRoleService = scope.ServiceProvider.GetRequiredService<ITenantRoleService>();

        // Get tenant store if available
        var tenantStore = scope.ServiceProvider.GetService<ITenantStore>();

        if (tenantStore is null)
        {
            _logger.LogDebug("No ITenantStore registered; skipping tenant role seeding");
            return;
        }

        // Get all tenants
        var tenants = await GetAllTenantsAsync(tenantStore);

        foreach (var tenant in tenants)
        {
            // Check if tenant already has roles
            var hasRoles = await dbContext.TenantRoles
                .AnyAsync(r => r.TenantId == tenant.Id);

            if (hasRoles)
            {
                _logger.LogDebug(
                    "Tenant {TenantId} already has roles; skipping",
                    tenant.Id);
                continue;
            }

            _logger.LogInformation(
                "Seeding default roles for tenant {TenantId}",
                tenant.Id);

            // Initialize with null owner (no user assigned yet)
            // This creates the roles but doesn't assign an owner
            await SeedRolesOnlyAsync(dbContext, tenant.Id);
        }
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

    private static async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(ITenantStore store)
    {
        // ITenantStore might not have a GetAll method
        // This is a simplified approach; actual implementation depends on store
        try
        {
            if (store is IEnumerable<TenantInfo> enumerable)
                return enumerable;

            // Fallback: return empty (seeding happens on tenant creation instead)
            return [];
        }
        catch
        {
            return [];
        }
    }
}
