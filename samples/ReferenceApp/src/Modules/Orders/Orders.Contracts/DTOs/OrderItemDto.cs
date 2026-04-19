namespace Orders.Contracts.DTOs;

/// <summary>Represents a single line item in a create-order request.</summary>
public sealed record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);
