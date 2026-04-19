namespace Nac.Identity.Management.Contracts;

/// <summary>
/// Status flags returned by <c>ITenantOnboardingService.OnboardAsync</c>.
/// </summary>
public enum OnboardingStatus
{
    /// <summary>Roles and optional owner membership were successfully seeded.</summary>
    Seeded,

    /// <summary>Tenant was already onboarded; no mutation performed.</summary>
    AlreadyOnboarded,
}

/// <summary>
/// Internal result of a tenant onboarding operation.
/// </summary>
public sealed record OnboardingResult(
    string TenantId,
    OnboardingStatus Status,
    IReadOnlyList<Guid> RoleIds,
    Guid? OwnerMembershipId);

/// <summary>
/// API-facing projection of <see cref="OnboardingResult"/> for the retry endpoint.
/// </summary>
public sealed record OnboardingResultDto(
    string TenantId,
    string Status,
    IReadOnlyList<Guid> RoleIds,
    Guid? OwnerMembershipId);
