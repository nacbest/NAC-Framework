using Nac.Core.Abstractions.Events;

namespace Nac.Identity.Management.Onboarding;

/// <summary>
/// Published after <c>TenantOnboardingService</c> successfully seeds roles
/// and (optionally) the Owner membership for a new tenant.
/// </summary>
public sealed record TenantOnboardedEvent(
    string TenantId,
    IReadOnlyList<Guid> RolesSeeded,
    Guid? OwnerMembershipId) : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
