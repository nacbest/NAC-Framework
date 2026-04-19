using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Results;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Memberships;
using Nac.Identity.Users;

namespace Nac.Identity.Management.Services;

/// <summary>
/// Orchestrates invitation, listing, role-change, and removal of tenant memberships.
/// Wraps <see cref="IMembershipService"/>; enforces current-tenant scoping via
/// <see cref="ICurrentUser.TenantId"/>. Cache invalidation is handled inside
/// <see cref="IMembershipService"/> (Phase 02).
/// </summary>
public sealed class MembershipManagementService(
    IMembershipService membershipService,
    UserManager<NacUser> userManager,
    ICurrentUser currentUser,
    ILogger<MembershipManagementService> logger)
{
    // ── Invite ────────────────────────────────────────────────────────────────

    public async Task<Result<InviteResponse>> InviteAsync(InviteRequest request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Result<InviteResponse>.Forbidden("No active tenant in current session.");

        // Reuse existing user or create a placeholder (no password — identity flow handled separately).
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            user = new NacUser(request.Email);
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => new ValidationError(e.Code, e.Description)).ToArray();
                return Result<InviteResponse>.Invalid(errors);
            }
            logger.LogInformation("Created placeholder user {UserId} for invite to tenant {TenantId}", user.Id, tenantId);
        }

        var (membershipId, token) = await membershipService.InviteAsync(
            user.Id, tenantId, currentUser.Id, request.RoleIds, ct);

        return Result<InviteResponse>.Success(new InviteResponse(membershipId, token));
    }

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<MembershipDto>>> ListAsync(CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Result<IReadOnlyList<MembershipDto>>.Forbidden("No active tenant in current session.");

        var memberships = await membershipService.ListMembersAsync(tenantId, ct);
        var dtos = await ProjectMembershipsAsync(memberships, ct);
        return Result<IReadOnlyList<MembershipDto>>.Success(dtos);
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    public async Task<Result> RemoveAsync(Guid membershipId, CancellationToken ct = default)
    {
        if (!await BelongsToCurrentTenantAsync(membershipId, ct))
            return Result.Forbidden("Membership does not belong to the current tenant.");

        try
        {
            await membershipService.RemoveMemberAsync(membershipId, ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.NotFound(ex.Message);
        }
    }

    // ── Change roles ──────────────────────────────────────────────────────────

    public async Task<Result> ChangeRolesAsync(Guid membershipId, ChangeMembershipRolesRequest request,
                                               CancellationToken ct = default)
    {
        if (!await BelongsToCurrentTenantAsync(membershipId, ct))
            return Result.Forbidden("Membership does not belong to the current tenant.");

        try
        {
            await membershipService.ChangeRolesAsync(membershipId, request.RoleIds, ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.NotFound(ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> BelongsToCurrentTenantAsync(Guid membershipId, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return false;

        // Resolve by listing tenant members and checking the id exists.
        var members = await membershipService.ListMembersAsync(tenantId, ct);
        return members.Any(m => m.Id == membershipId);
    }

    private async Task<IReadOnlyList<MembershipDto>> ProjectMembershipsAsync(
        IReadOnlyList<MembershipSummary> memberships, CancellationToken ct)
    {
        var result = new List<MembershipDto>(memberships.Count);
        foreach (var m in memberships)
        {
            var user = await userManager.FindByIdAsync(m.UserId.ToString());
            result.Add(new MembershipDto(
                m.Id, m.UserId, user?.Email, m.TenantId,
                m.Status.ToString(), m.RoleIds, m.JoinedAt, m.IsDefault));
        }
        return result;
    }
}
