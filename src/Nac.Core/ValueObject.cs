namespace Nac.Core;

/// <summary>
/// Base class for value objects — immutable types defined by their property values, not identity.
/// Two value objects with the same properties are considered equal.
/// Override <see cref="GetEqualityComponents"/> to define which properties participate in equality.
/// </summary>
/// <example>
/// <code>
/// public sealed class Money : ValueObject
/// {
///     public decimal Amount { get; }
///     public string Currency { get; }
///
///     public Money(decimal amount, string currency)
///     {
///         Amount = amount;
///         Currency = currency;
///     }
///
///     protected override IEnumerable&lt;object?&gt; GetEqualityComponents()
///     {
///         yield return Amount;
///         yield return Currency;
///     }
/// }
/// </code>
/// </example>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the components used for equality comparison.
    /// All components are compared in order.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || GetType() != other.GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
        => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
            hash.Add(component);
        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !Equals(left, right);
}
