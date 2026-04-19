using Nac.Core.Domain;
using Nac.Core.Primitives;

namespace Nac.Core.ValueObjects;

public sealed class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = Guard.NotNullOrEmpty(currency, nameof(currency)).ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor) =>
        new(money.Amount * factor, money.Currency);

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on Money with different currencies: {left.Currency} vs {right.Currency}");
    }

    public override string ToString() => $"{Amount} {Currency}";
}
