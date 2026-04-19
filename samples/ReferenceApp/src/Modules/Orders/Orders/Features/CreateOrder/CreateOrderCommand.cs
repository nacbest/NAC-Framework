using Nac.Core.Results;
using Nac.Cqrs.Commands;
using Nac.Cqrs.Markers;
using Orders.Contracts.DTOs;

namespace Orders.Features.CreateOrder;

/// <summary>
/// Command to create a new order.
/// <see cref="ITransactionalCommand"/> opts this into <c>TransactionBehavior</c>
/// which calls <c>IUnitOfWork.SaveChangesAsync</c> after the handler succeeds —
/// triggering OutboxInterceptor to write the <c>OrderCreatedEvent</c> outbox row
/// in the same DB transaction.
/// </summary>
internal sealed record CreateOrderCommand(List<OrderItemDto> Items)
    : ICommand<Result<Guid>>, ITransactionalCommand;
