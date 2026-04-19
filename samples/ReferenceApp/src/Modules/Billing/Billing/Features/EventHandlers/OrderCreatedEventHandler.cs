using Billing.Domain;
using Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nac.EventBus.Abstractions;
using Nac.MultiTenancy.Abstractions;
using Orders.Contracts.IntegrationEvents;

namespace Billing.Features.EventHandlers;

/// <summary>
/// Handles <see cref="OrderCreatedEvent"/> published by the Orders module via the transactional outbox.
/// Upserts a <see cref="Customer"/> (on first order) and creates an <see cref="Invoice"/>.
///
/// Tenant context: OutboxWorker runs as a background service — AsyncLocal TenantId is lost.
/// We restore it by calling <c>ITenantContext.SetCurrentTenant</c> from the event payload
/// BEFORE any DB operation so EF query filters and interceptors see the correct tenant.
///
/// Idempotency: unique index on Invoice.OrderId is the DB-layer guard.
/// AnyAsync check is the fast-path guard that avoids a duplicate-key exception on redelivery.
/// </summary>
internal sealed class OrderCreatedEventHandler(
    BillingDbContext db,
    ITenantContext tenant)
    : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct = default)
    {
        // 1. Restore tenant context — background worker loses AsyncLocal propagation (red-team C4).
        //    TenantInfo.Name is not carried in event payload; use TenantId as placeholder.
        tenant.SetCurrentTenant(new TenantInfo
        {
            Id   = @event.TenantId,
            Name = @event.TenantId,
        });

        // 2. Idempotency guard — unique index on Invoice.OrderId is the DB-level safety net,
        //    but this check avoids a round-trip exception on redelivery.
        var invoiceExists = await db.Invoices
            .AnyAsync(i => i.OrderId == @event.OrderId, ct);

        if (invoiceExists)
            return;

        // 3. Upsert customer — create on first order from this user+tenant combination.
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.UserId == @event.CustomerId, ct);

        if (customer is null)
        {
            customer = new Customer
            {
                Id        = Guid.NewGuid(),
                UserId    = @event.CustomerId,
                Email     = $"{@event.CustomerId}@unknown.local", // hydrate via IIdentityService if needed
                TenantId  = @event.TenantId,
                Plan      = Plan.Free,
                CreatedAt = DateTime.UtcNow,
            };
            db.Customers.Add(customer);
        }

        // 4. Create invoice — status starts as Pending awaiting payment confirmation.
        db.Invoices.Add(new Invoice
        {
            Id         = Guid.NewGuid(),
            CustomerId = customer.Id,
            OrderId    = @event.OrderId,
            Amount     = @event.Total,
            Status     = InvoiceStatus.Pending,
            TenantId   = @event.TenantId,
            CreatedAt  = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }
}
