using FluentAssertions;
using Nac.Core.ValueObjects;
using Xunit;

namespace Nac.Core.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidAmount_CreatesInstance()
    {
        // Act
        var money = new Money(100, "USD");

        // Assert
        money.Amount.Should().Be(100);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WithLowercaseCurrency_ConvertsToUppercase()
    {
        // Act
        var money = new Money(100, "usd");

        // Assert
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WithNullCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => new Money(100, null!);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => new Money(100, "");
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Zero_CreatesMoneyWithZeroAmount()
    {
        // Act
        var money = Money.Zero("USD");

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_WithSameCurrency_AddsAmounts()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Addition_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act & Assert
        var action = () => money1 + money2;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtraction_WithSameCurrency_SubtractsAmounts()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(30, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(70);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Subtraction_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act & Assert
        var action = () => money1 - money2;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Multiplication_WithFactor_MultipliesAmount()
    {
        // Arrange
        var money = new Money(100, "USD");

        // Act
        var result = money * 2.5m;

        // Assert
        result.Amount.Should().Be(250);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Multiplication_WithZeroFactor_ResultsInZero()
    {
        // Arrange
        var money = new Money(100, "USD");

        // Act
        var result = money * 0;

        // Assert
        result.Amount.Should().Be(0);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void GreaterThan_WithSameCurrency_ComparesAmounts()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act & Assert
        (money1 > money2).Should().BeTrue();
        (money2 > money1).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act & Assert
        var action = () => money1 > money2;
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LessThan_WithSameCurrency_ComparesAmounts()
    {
        // Arrange
        var money1 = new Money(50, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        (money1 < money2).Should().BeTrue();
        (money2 < money1).Should().BeFalse();
    }

    [Fact]
    public void GreaterThanOrEqual_WithEqualAmounts_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        (money1 >= money2).Should().BeTrue();
    }

    [Fact]
    public void LessThanOrEqual_WithEqualAmounts_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        (money1 <= money2).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_WithEqualAmounts_ReturnsZero()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        money1.CompareTo(money2).Should().Be(0);
    }

    [Fact]
    public void CompareTo_WithGreaterAmount_ReturnsPositive()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act & Assert
        money1.CompareTo(money2).Should().BePositive();
    }

    [Fact]
    public void CompareTo_WithLesserAmount_ReturnsNegative()
    {
        // Arrange
        var money1 = new Money(50, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        money1.CompareTo(money2).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_WithNull_ReturnsPositive()
    {
        // Arrange
        var money = new Money(100, "USD");

        // Act & Assert
        money.CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void CompareTo_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act & Assert
        var action = () => money1.CompareTo(money2);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Equality_WithSameAmountAndCurrency_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentAmount_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act & Assert
        (money1 == money2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentCurrency_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "EUR");

        // Act & Assert
        (money1 == money2).Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var money = new Money(100.50m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Match("*100*50*");
        result.Should().Contain("USD");
    }

    [Fact]
    public void GetHashCode_WithSameAmountAndCurrency_ReturnsSameValue()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void CanBeUsedInSet()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");
        var set = new HashSet<Money> { money1 };

        // Act
        set.Add(money2);

        // Assert
        set.Should().HaveCount(1);
    }
}
