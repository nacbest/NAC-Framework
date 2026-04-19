namespace Orders.Domain;

/// <summary>Lifecycle states of an order.</summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}
