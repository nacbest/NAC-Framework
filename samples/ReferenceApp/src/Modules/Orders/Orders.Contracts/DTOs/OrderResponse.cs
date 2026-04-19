namespace Orders.Contracts.DTOs;

/// <summary>Public read model returned by GET /api/orders/{id}.</summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    decimal Total,
    string Status,
    DateTime CreatedAt);
