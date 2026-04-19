using Nac.Core.Abstractions.Identity;
using Nac.Core.Results;
using Nac.Cqrs.Commands;
using Nac.MultiTenancy.Abstractions;
using Orders.Domain;
using Orders.Infrastructure;

namespace Orders.Features.CreateOrder;

/// <summary>
/// Handles <see cref="CreateOrderCommand"/>. Maps DTOs → domain items, calls
/// <see cref="Order.Create"/> (raises OrderCreatedEvent), persists via repository.
/// SaveChangesAsync is called by <c>TransactionBehavior</c> (ITransactionalCommand),
/// which triggers OutboxInterceptor to write the event row in the same transaction.
/// </summary>
internal sealed class CreateOrderHandler(
    IOrderRepository repository,
    ICurrentUser currentUser,
    ITenantContext tenantContext)
    : ICommandHandler<CreateOrderCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> HandleAsync(
        CreateOrderCommand command,
        CancellationToken ct = default)
    {
        // Resolve tenant — TenantEntityInterceptor will also stamp via ITenantEntity,
        // but we pass it explicitly to Order.Create so it ends up in the event payload.
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("No active tenant context.");

        var items = command.Items.Select(dto =>
            new OrderItem(dto.ProductId, dto.Quantity, dto.UnitPrice));

        var order = Order.Create(currentUser.Id, items, tenantId);

        await repository.AddAsync(order, ct);

        // TransactionBehavior calls IUnitOfWork.SaveChangesAsync after this returns.
        return Result<Guid>.Success(order.Id);
    }
}
