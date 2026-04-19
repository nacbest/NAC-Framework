namespace Nac.Core.Abstractions.Events;

public sealed record UserRegisteredEvent(
    Guid UserId, string Email, string TenantId)
    : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
