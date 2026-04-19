using Nac.Core.Domain;

namespace Orders.Domain;

/// <summary>Centralised error factory for the Orders domain.</summary>
internal static class OrderErrors
{
    public static DomainError NotFound(Guid id) =>
        DomainError.NotFound(nameof(Order), id);

    public static DomainError EmptyItems() =>
        DomainError.Validation("Order.EmptyItems", "An order must contain at least one item.");

    public static DomainError InvalidQuantity(Guid productId) =>
        DomainError.Validation("Order.InvalidQuantity", $"Item for product '{productId}' must have quantity > 0.");

    public static DomainError InvalidUnitPrice(Guid productId) =>
        DomainError.Validation("Order.InvalidUnitPrice", $"Item for product '{productId}' must have unit price >= 0.");
}
