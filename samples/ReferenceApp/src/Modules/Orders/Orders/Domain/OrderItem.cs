namespace Orders.Domain;

/// <summary>
/// Value-object-like line item owned by an <see cref="Order"/>.
/// Stored as an owned entity (EF Core OwnsMany) — no independent identity.
/// </summary>
internal sealed class OrderItem
{
    // Parameterless ctor required by EF Core owned-entity materialisation.
    private OrderItem() { }

    internal OrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    /// <summary>Computed line total — not persisted, derived on read.</summary>
    public decimal LineTotal => Quantity * UnitPrice;
}
