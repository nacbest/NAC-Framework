using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;
using Nac.MultiTenancy.Management.Abstractions;

namespace Nac.MultiTenancy.Management.Domain.Events;

/// <summary>
/// Raised when a new tenant aggregate has been created.
/// Implements both <see cref="IDomainEvent"/> (in-process) and
/// <see cref="IIntegrationEvent"/> so that the outbox interceptor persists it
/// for at-least-once cross-process delivery.
/// </summary>
public sealed record TenantCreatedEvent(
    Guid Id,
    string Identifier,
    string Name,
    TenantIsolationMode IsolationMode,
    Guid? CreatedByUserId = null) : IDomainEvent, IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
