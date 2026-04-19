using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nac.Identity.Context;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Memberships;
using Nac.Identity.Roles;
using Nac.Identity.RoleTemplates;

namespace Nac.Identity.Management.Onboarding;

/// <summary>
/// Idempotent implementation of <see cref="ITenantOnboardingService"/>.
/// Clones Owner/Admin/Member system templates into the tenant and optionally
/// creates an Owner membership for the creator user.
///
/// Idempotency gate: presence of any <c>NacRole</c> with
/// <c>TenantId = tenantId</c> and <c>BaseTemplateId = ownerTemplateId</c>
/// signals that onboarding already ran; returns <see cref="OnboardingStatus.AlreadyOnboarded"/>
/// without any mutation.
///
/// Host-creator skip: if the creator resolves to a user with <c>IsHost = true</c>,
/// only roles are seeded — no Owner membership is created
/// (host users have cross-tenant access and must not be scoped as tenant owners).
/// </summary>
internal sealed class TenantOnboardingService(
    IRoleService roleService,
    IMembershipService membershipService,
    RoleTemplateDefinitionManager templateManager,
    NacIdentityDbContext db,
    ILogger<TenantOnboardingService> logger) : ITenantOnboardingService
{
    // Keys used by DefaultRoleTemplateProvider.
    private const string OwnerKey = "owner";
    private const string AdminKey = "admin";
    private const string MemberKey = "member";

    /// <inheritdoc />
    public async Task<OnboardingResult> OnboardAsync(
        string tenantId, Guid? creatorUserId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // ── 1. Resolve template ids ───────────────────────────────────────────
        var ownerTemplateId = ResolveTemplateId(OwnerKey);
        var adminTemplateId = ResolveTemplateId(AdminKey);
        var memberTemplateId = ResolveTemplateId(MemberKey);

        // ── 2. Idempotency check ─────────────────────────────────────────────
        var alreadySeeded = await db.Roles.AsNoTracking()
            .AnyAsync(r => r.TenantId == tenantId
                        && r.BaseTemplateId == ownerTemplateId
                        && !r.IsDeleted, ct);

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Tenant {TenantId} is already onboarded — skipping.", tenantId);
            return new OnboardingResult(tenantId, OnboardingStatus.AlreadyOnboarded, [], null);
        }

        // ── 3. Clone templates + seed membership atomically ──────────────────
        // Single transaction so a partial failure (e.g. owner succeeds, admin fails)
        // rolls back all three clones — retry is safe because idempotency gate
        // will find no owner role on next run.
        logger.LogInformation("Onboarding tenant {TenantId}: cloning role templates.", tenantId);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ownerRole = await roleService.CloneFromTemplateAsync(tenantId, ownerTemplateId, "Owner", ct);
        var adminRole = await roleService.CloneFromTemplateAsync(tenantId, adminTemplateId, "Admin", ct);
        var memberRole = await roleService.CloneFromTemplateAsync(tenantId, memberTemplateId, "Member", ct);

        var roleIds = new List<Guid> { ownerRole.Id, adminRole.Id, memberRole.Id };

        Guid? ownerMembershipId = null;

        if (creatorUserId.HasValue)
        {
            var isHost = await IsHostUserAsync(creatorUserId.Value, ct);
            if (isHost)
            {
                // Host accounts must NOT become tenant owners — they have cross-tenant access.
                logger.LogInformation(
                    "Creator {UserId} is a host account — skipping Owner membership for tenant {TenantId}.",
                    creatorUserId, tenantId);
            }
            else
            {
                ownerMembershipId = await membershipService.CreateActiveMembershipAsync(
                    creatorUserId.Value, tenantId, [ownerRole.Id], isDefault: true, ct);

                logger.LogInformation(
                    "Created Owner membership {MembershipId} for user {UserId} in tenant {TenantId}.",
                    ownerMembershipId, creatorUserId, tenantId);
            }
        }

        await tx.CommitAsync(ct);

        return new OnboardingResult(tenantId, OnboardingStatus.Seeded, roleIds, ownerMembershipId);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Guid ResolveTemplateId(string key)
    {
        if (!templateManager.Contains(key))
            throw new InvalidOperationException(
                $"Role template '{key}' is not registered. Ensure RoleTemplateSeeder has run.");

        return RoleTemplateKeyHasher.ToGuid(key);
    }

    private async Task<bool> IsHostUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user?.IsHost ?? false;
    }
}
