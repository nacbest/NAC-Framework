using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;

namespace Orders.Contracts.IntegrationEvents;

/// <summary>
/// Dual-role event: implements both <see cref="IDomainEvent"/> (raised on Order aggregate)
/// and <see cref="IIntegrationEvent"/> (harvested by OutboxInterceptor on SaveChanges).
/// A single class removes the need for a domain→integration bridge handler.
/// OutboxInterceptor scans IHasDomainEvents.DomainEvents → casts each to IIntegrationEvent
/// → writes outbox row. DomainEventInterceptor runs AFTER save and dispatches in-process;
/// it finds no IDomainEventDispatcher registered by default so this is a no-op.
/// Net effect: one channel (outbox), no double-dispatch risk.
/// TenantId is included in the payload so downstream Billing module can set tenant context.
/// </summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    string TenantId,
    decimal Total,
    DateTime OccurredOn,
    Guid EventId) : IDomainEvent, IIntegrationEvent;
