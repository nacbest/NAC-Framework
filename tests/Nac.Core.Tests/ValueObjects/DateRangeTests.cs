using FluentAssertions;
using Nac.Core.ValueObjects;
using Xunit;

namespace Nac.Core.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WithValidDates_CreatesInstance()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 31);

        // Act
        var range = new DateRange(start, end);

        // Assert
        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Constructor_WithEqualDates_CreatesInstance()
    {
        // Arrange
        var date = new DateTime(2025, 1, 1);

        // Act
        var range = new DateRange(date, date);

        // Assert
        range.Start.Should().Be(date);
        range.End.Should().Be(date);
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_ThrowsArgumentException()
    {
        // Arrange
        var start = new DateTime(2025, 1, 31);
        var end = new DateTime(2025, 1, 1);

        // Act & Assert
        var action = () => new DateRange(start, end);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Contains_WithDateInRange_ReturnsTrue()
    {
        // Arrange
        var range = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var date = new DateTime(2025, 1, 15);

        // Act
        var result = range.Contains(date);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_WithDateAtStart_ReturnsTrue()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1);
        var range = new DateRange(start, new DateTime(2025, 1, 31));

        // Act
        var result = range.Contains(start);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_WithDateAtEnd_ReturnsTrue()
    {
        // Arrange
        var end = new DateTime(2025, 1, 31);
        var range = new DateRange(new DateTime(2025, 1, 1), end);

        // Act
        var result = range.Contains(end);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_WithDateBeforeRange_ReturnsFalse()
    {
        // Arrange
        var range = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var date = new DateTime(2024, 12, 31);

        // Act
        var result = range.Contains(date);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_WithDateAfterRange_ReturnsFalse()
    {
        // Arrange
        var range = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var date = new DateTime(2025, 2, 1);

        // Act
        var result = range.Contains(date);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WithCompletelyOverlappingRange_ReturnsTrue()
    {
        // Arrange
        var range1 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var range2 = new DateRange(new DateTime(2025, 1, 10), new DateTime(2025, 1, 20));

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithPartialOverlap_ReturnsTrue()
    {
        // Arrange
        var range1 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 15));
        var range2 = new DateRange(new DateTime(2025, 1, 10), new DateTime(2025, 1, 31));

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithAdjacentRanges_ReturnsTrue()
    {
        // Arrange
        var range1 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 15));
        var range2 = new DateRange(new DateTime(2025, 1, 15), new DateTime(2025, 1, 31));

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithNoOverlap_ReturnsFalse()
    {
        // Arrange
        var range1 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 10));
        var range2 = new DateRange(new DateTime(2025, 1, 20), new DateTime(2025, 1, 31));

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Overlaps_WithIdenticalRanges_ReturnsTrue()
    {
        // Arrange
        var date1 = new DateTime(2025, 1, 1);
        var date2 = new DateTime(2025, 1, 31);
        var range1 = new DateRange(date1, date2);
        var range2 = new DateRange(date1, date2);

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Duration_ReturnsTimeSpan()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 31);
        var range = new DateRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(end - start);
    }

    [Fact]
    public void Duration_WithSameDate_ReturnsZero()
    {
        // Arrange
        var date = new DateTime(2025, 1, 1);
        var range = new DateRange(date, date);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Duration_CalculatesCorrectly()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1, 12, 0, 0);
        var end = new DateTime(2025, 1, 1, 14, 30, 0);
        var range = new DateRange(start, end);

        // Act
        var duration = range.Duration;

        // Assert
        duration.Should().Be(TimeSpan.FromHours(2.5));
    }

    [Fact]
    public void Equality_WithSameDates_ReturnsTrue()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 31);
        var range1 = new DateRange(start, end);
        var range2 = new DateRange(start, end);

        // Act & Assert
        (range1 == range2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentStart_ReturnsFalse()
    {
        // Arrange
        var range1 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var range2 = new DateRange(new DateTime(2025, 1, 2), new DateTime(2025, 1, 31));

        // Act & Assert
        (range1 == range2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentEnd_ReturnsFalse()
    {
        // Arrange
        var range1 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var range2 = new DateRange(new DateTime(2025, 1, 1), new DateTime(2025, 1, 30));

        // Act & Assert
        (range1 == range2).Should().BeFalse();
    }

    [Fact]
    public void CanBeUsedInSet()
    {
        // Arrange
        var date1 = new DateTime(2025, 1, 1);
        var date2 = new DateTime(2025, 1, 31);
        var range1 = new DateRange(date1, date2);
        var range2 = new DateRange(date1, date2);
        var set = new HashSet<DateRange> { range1 };

        // Act
        set.Add(range2);

        // Assert
        set.Should().HaveCount(1);
    }

    [Fact]
    public void GetHashCode_WithSameDates_ReturnsSameValue()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 1, 31);
        var range1 = new DateRange(start, end);
        var range2 = new DateRange(start, end);

        // Act & Assert
        range1.GetHashCode().Should().Be(range2.GetHashCode());
    }
}
