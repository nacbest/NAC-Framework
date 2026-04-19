using Nac.Identity.Management.Contracts;

namespace Nac.Identity.Management.Onboarding;

/// <summary>
/// Idempotent service that seeds tenant-scoped roles cloned from system templates
/// (Owner, Admin, Member) and optionally creates an Owner membership for the creator.
/// Safe to call multiple times — repeated calls return <see cref="OnboardingStatus.AlreadyOnboarded"/>.
/// </summary>
public interface ITenantOnboardingService
{
    /// <summary>
    /// Seeds tenant roles from system templates and optionally assigns the creator as Owner.
    /// </summary>
    /// <param name="tenantId">The tenant slug to onboard.</param>
    /// <param name="creatorUserId">
    /// User to assign Owner membership. Pass <c>null</c> when creator is unknown or
    /// is a host account (host check is performed internally).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Onboarding result indicating action taken and ids created.</returns>
    Task<OnboardingResult> OnboardAsync(string tenantId, Guid? creatorUserId, CancellationToken ct = default);
}
