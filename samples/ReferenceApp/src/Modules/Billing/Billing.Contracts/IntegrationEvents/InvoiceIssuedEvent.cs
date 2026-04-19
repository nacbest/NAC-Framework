using Nac.Core.Abstractions.Events;

namespace Billing.Contracts.IntegrationEvents;

/// <summary>
/// Published after an invoice is created. Allows downstream modules to react
/// to billing events (e.g., send confirmation email, update analytics).
/// Included for future-downstream demo — not consumed in Phase 04 scope.
/// </summary>
public sealed record InvoiceIssuedEvent(
    Guid InvoiceId,
    Guid OrderId,
    Guid CustomerId,
    string TenantId,
    decimal Amount,
    DateTime OccurredOn,
    Guid EventId) : IIntegrationEvent;
