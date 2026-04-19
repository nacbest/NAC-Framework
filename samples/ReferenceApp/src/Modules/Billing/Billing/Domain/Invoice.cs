namespace Billing.Domain;

/// <summary>
/// Represents a billing invoice generated from an order.
/// Simple class — no AggregateRoot needed for this KISS module.
///
/// WEAK REFERENCE: OrderId is a logical reference only — no SQL foreign key to the
/// orders schema. Cross-schema FK is an anti-pattern in modular monoliths; integrity
/// is maintained at the application layer (event handler idempotency + unique index).
///
/// TenantId is stamped explicitly (defense-in-depth alongside TenantEntityInterceptor).
/// </summary>
internal sealed class Invoice
{
    public Guid          Id         { get; init; }
    public Guid          CustomerId { get; init; }

    /// <summary>
    /// Logical reference to Orders.Order — no FK constraint (cross-schema reference).
    /// Unique index on this column provides idempotency when OutboxWorker redelivers.
    /// </summary>
    public Guid          OrderId    { get; init; }

    public decimal       Amount     { get; init; }
    public InvoiceStatus Status     { get; set; } = InvoiceStatus.Pending;
    public string        TenantId   { get; init; } = string.Empty;
    public DateTime      CreatedAt  { get; init; }
}
