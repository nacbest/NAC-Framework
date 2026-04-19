using Microsoft.Extensions.Logging;
using Nac.EventBus.Abstractions;
using Nac.Identity.Management.Contracts;
using Nac.MultiTenancy.Management.Domain.Events;

namespace Nac.Identity.Management.Onboarding;

/// <summary>
/// Event bus handler for <see cref="TenantCreatedEvent"/>. Triggers tenant
/// onboarding (role cloning + optional owner membership) in a decoupled,
/// at-least-once delivery model. Idempotency is enforced inside
/// <see cref="ITenantOnboardingService.OnboardAsync"/>.
///
/// On failure: logs the exception and publishes
/// <see cref="TenantOnboardingFailedEvent"/> so operators can observe the gap
/// and trigger retry via <c>POST /api/identity/tenants/{id}/onboard</c>.
/// </summary>
internal sealed class TenantOnboardingHandler(
    ITenantOnboardingService onboardingService,
    IEventPublisher eventPublisher,
    ILogger<TenantOnboardingHandler> logger) : IEventHandler<TenantCreatedEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(TenantCreatedEvent @event, CancellationToken ct = default)
    {
        var tenantId = @event.Identifier;

        logger.LogInformation(
            "TenantOnboardingHandler: received TenantCreatedEvent for tenant {TenantId} (creator={CreatorId}).",
            tenantId, @event.CreatedByUserId);

        try
        {
            var result = await onboardingService.OnboardAsync(tenantId, @event.CreatedByUserId, ct);

            if (result.Status == OnboardingStatus.AlreadyOnboarded)
            {
                logger.LogInformation(
                    "Tenant {TenantId} already onboarded — no action taken.", tenantId);
                return;
            }

            await eventPublisher.PublishAsync(
                new TenantOnboardedEvent(tenantId, result.RoleIds, result.OwnerMembershipId), ct);

            logger.LogInformation(
                "Tenant {TenantId} onboarded: {RoleCount} roles seeded, membershipId={MembershipId}.",
                tenantId, result.RoleIds.Count, result.OwnerMembershipId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Tenant onboarding failed for tenant {TenantId}. Raising TenantOnboardingFailedEvent.",
                tenantId);

            // Best-effort failure notification — do not rethrow so the event bus
            // does not retry indefinitely on a persistent schema error.
            try
            {
                await eventPublisher.PublishAsync(
                    new TenantOnboardingFailedEvent(tenantId, ex.Message), ct);
            }
            catch (Exception publishEx)
            {
                logger.LogError(publishEx,
                    "Failed to publish TenantOnboardingFailedEvent for tenant {TenantId}.", tenantId);
            }
        }
    }
}
