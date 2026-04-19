namespace Nac.Core.Abstractions.Events;

public sealed record PasswordResetEvent(Guid UserId, string Email)
    : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
