namespace Orders.Contracts.DTOs;

/// <summary>HTTP request body for POST /api/orders.</summary>
public sealed record CreateOrderRequest(List<OrderItemDto> Items);
