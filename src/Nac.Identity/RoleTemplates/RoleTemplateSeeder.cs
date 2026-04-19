using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nac.Identity.Context;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Users;

namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Hosted service that idempotently seeds <see cref="NacRole"/> template rows and their
/// <see cref="PermissionGrant"/> rows on every application start. Waits up to 10 seconds
/// for the database to become available before aborting. Each template is seeded inside
/// its own transaction so a single failure does not block other templates.
/// </summary>
internal sealed class RoleTemplateSeeder(
    IServiceScopeFactory scopeFactory,
    RoleTemplateDefinitionManager templateManager,
    ILogger<RoleTemplateSeeder> logger) : IHostedService
{
    private const int MaxRetryMs = 10_000;
    private const int RetryDelayMs = 500;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NacIdentityDbContext>();
        var permManager = scope.ServiceProvider.GetRequiredService<PermissionDefinitionManager>();

        await WaitForDatabaseAsync(db, cancellationToken);

        foreach (var definition in templateManager.GetAll())
        {
            try
            {
                await SeedTemplateAsync(db, permManager, definition, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed role template '{Key}'.", definition.Key);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── private helpers ───────────────────────────────────────────────────────

    private async Task WaitForDatabaseAsync(NacIdentityDbContext db, CancellationToken ct)
    {
        var elapsed = 0;
        while (!await db.Database.CanConnectAsync(ct))
        {
            if (elapsed >= MaxRetryMs)
                throw new TimeoutException(
                    $"Database was not reachable after {MaxRetryMs}ms. Role template seeding aborted.");

            logger.LogWarning("Database not ready — retrying in {Delay}ms...", RetryDelayMs);
            await Task.Delay(RetryDelayMs, ct);
            elapsed += RetryDelayMs;
        }
    }

    private async Task SeedTemplateAsync(
        NacIdentityDbContext db,
        PermissionDefinitionManager permManager,
        RoleTemplateDefinition definition,
        CancellationToken ct)
    {
        var templateId = RoleTemplateKeyHasher.ToGuid(definition.Key);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // ── 1. Upsert the NacRole template row ────────────────────────────────
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == templateId, ct);
        if (role is null)
        {
            // Use public constructor then override Id to the deterministic hash value
            // so the seeder is idempotent across boots (PK upsert by stable Guid).
            role = new NacRole(definition.Key, tenantId: null, isTemplate: true, definition.Description)
            {
                Id = templateId,
                NormalizedName = definition.Key.ToUpperInvariant(),
            };
            db.Roles.Add(role);
            logger.LogInformation("Seeding role template '{Key}' (id={Id}).", definition.Key, templateId);
        }
        else
        {
            // Refresh mutable fields in case provider was updated.
            role.Description = definition.Description;
        }

        // ── 2. Load existing grants for this template ─────────────────────────
        var existingGrants = await db.PermissionGrants
            .Where(g => g.ProviderName == PermissionProviderNames.Role
                     && g.ProviderKey == templateId.ToString()
                     && g.TenantId == null)
            .ToListAsync(ct);

        var existingNames = existingGrants.Select(g => g.PermissionName).ToHashSet(StringComparer.Ordinal);

        // ── 3. Validate permission names against PermissionDefinitionManager ──
        var validNames = new List<string>();
        foreach (var perm in definition.PermissionNames)
        {
            if (permManager.GetOrNull(perm) is not null)
            {
                validNames.Add(perm);
            }
            else
            {
                logger.LogWarning(
                    "Role template '{Key}': permission '{Perm}' is not registered in PermissionDefinitionManager — skipping.",
                    definition.Key, perm);
            }
        }

        var desiredNames = validNames.ToHashSet(StringComparer.Ordinal);

        // ── 4. Insert new grants ───────────────────────────────────────────────
        foreach (var perm in desiredNames.Except(existingNames))
        {
            db.PermissionGrants.Add(
                new PermissionGrant(PermissionProviderNames.Role, templateId.ToString(), perm, tenantId: null));
        }

        // ── 5. Remove stale grants (template drift) ───────────────────────────
        foreach (var stale in existingGrants.Where(g => !desiredNames.Contains(g.PermissionName)))
        {
            db.PermissionGrants.Remove(stale);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
