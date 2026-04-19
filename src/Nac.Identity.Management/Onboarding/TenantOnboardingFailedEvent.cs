using Nac.Core.Abstractions.Events;

namespace Nac.Identity.Management.Onboarding;

/// <summary>
/// Published when <c>TenantOnboardingHandler</c> catches an unhandled exception
/// during tenant onboarding. The tenant exists but roles/membership were NOT seeded.
/// Operators should use the retry endpoint to recover.
/// </summary>
public sealed record TenantOnboardingFailedEvent(
    string TenantId,
    string Reason) : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
