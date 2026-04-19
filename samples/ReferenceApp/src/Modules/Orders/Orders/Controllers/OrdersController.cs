using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nac.Cqrs.Dispatching;
using Orders.Contracts.DTOs;
using Orders.Features.CreateOrder;
using Orders.Features.GetOrderById;
using ReferenceApp.SharedKernel.Results;

namespace Orders.Controllers;

/// <summary>HTTP entry point for the Orders module.</summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>Creates a new order for the authenticated user.</summary>
    [HttpPost]
    [Authorize(Policy = "Orders.Create")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrderCommand(request.Items);
        var result = await sender.SendAsync<Nac.Core.Results.Result<Guid>>(command, ct);

        if (!result.IsSuccess)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value }, result.Value);
    }

    /// <summary>Returns an order by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Orders.View")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await sender.SendAsync<Nac.Core.Results.Result<OrderResponse>>(query, ct);
        return result.ToActionResult();
    }
}
