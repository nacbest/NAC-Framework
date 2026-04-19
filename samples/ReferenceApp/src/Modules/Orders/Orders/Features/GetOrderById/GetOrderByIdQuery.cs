using Nac.Core.Results;
using Nac.Cqrs.Queries;
using Orders.Contracts.DTOs;

namespace Orders.Features.GetOrderById;

/// <summary>Query to fetch a single order by its identifier.</summary>
internal sealed record GetOrderByIdQuery(Guid Id) : IQuery<Result<OrderResponse>>;
