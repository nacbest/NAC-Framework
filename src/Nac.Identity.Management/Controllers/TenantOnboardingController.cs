using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nac.Core.Abstractions.Identity;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Management.Onboarding;

namespace Nac.Identity.Management.Controllers;

/// <summary>
/// Retry endpoint for tenant onboarding. Host-admin only.
/// Calls <see cref="ITenantOnboardingService.OnboardAsync"/> which is idempotent,
/// so replaying a successful onboarding is safe.
///
/// Authorization: currently gates on <c>ICurrentUser.IsHost</c> inline.
/// Phase 07 will introduce the <c>Host.AccessAllTenants</c> policy — replace
/// the inline check with <c>[Authorize(Policy = "Host.AccessAllTenants")]</c> then.
/// </summary>
[ApiController]
[Route("api/identity/tenants")]
[Authorize]
public sealed class TenantOnboardingController(
    ITenantOnboardingService onboardingService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Retries onboarding for a tenant. Idempotent — safe to call multiple times.
    /// Returns 200 with <see cref="OnboardingResultDto"/> on success.
    /// </summary>
    /// <param name="tenantId">Tenant identifier (slug or surrogate id string).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{tenantId}/onboard")]
    public async Task<IActionResult> Onboard(string tenantId, CancellationToken ct)
    {
        // TODO(Phase-07): Replace with [Authorize(Policy = "Host.AccessAllTenants")] attribute
        // once the Host permission policy is registered in AddNacIdentityManagement.
        if (!currentUser.IsHost)
            return Forbid();

        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest("tenantId is required.");

        try
        {
            var result = await onboardingService.OnboardAsync(tenantId, creatorUserId: null, ct);
            var dto = new OnboardingResultDto(
                result.TenantId,
                result.Status.ToString(),
                result.RoleIds,
                result.OwnerMembershipId);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            // Template not found or other domain invariant violation.
            return UnprocessableEntity(new { error = ex.Message });
        }
    }
}
