using FluentAssertions;
using Nac.Core.ValueObjects;
using Xunit;

namespace Nac.Core.Tests.ValueObjects;

public class PaginationTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        // Act
        var pagination = new Pagination(2, 50);

        // Assert
        pagination.PageNumber.Should().Be(2);
        pagination.PageSize.Should().Be(50);
    }

    [Fact]
    public void Constructor_WithDefaults_UsesDefaultPageSize()
    {
        // Act
        var pagination = new Pagination();

        // Assert
        pagination.PageNumber.Should().Be(1);
        pagination.PageSize.Should().Be(Pagination.DefaultPageSize);
    }

    [Fact]
    public void Constructor_WithZeroPageNumber_ClampsToOne()
    {
        // Act
        var pagination = new Pagination(0, 20);

        // Assert
        pagination.PageNumber.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNegativePageNumber_ClampsToOne()
    {
        // Act
        var pagination = new Pagination(-5, 20);

        // Assert
        pagination.PageNumber.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithZeroPageSize_UsesDefaultPageSize()
    {
        // Act
        var pagination = new Pagination(1, 0);

        // Assert
        pagination.PageSize.Should().Be(Pagination.DefaultPageSize);
    }

    [Fact]
    public void Constructor_WithNegativePageSize_UsesDefaultPageSize()
    {
        // Act
        var pagination = new Pagination(1, -10);

        // Assert
        pagination.PageSize.Should().Be(Pagination.DefaultPageSize);
    }

    [Fact]
    public void Constructor_WithPageSizeExceedingMax_ClampsToMax()
    {
        // Act
        var pagination = new Pagination(1, 200);

        // Assert
        pagination.PageSize.Should().Be(Pagination.MaxPageSize);
    }

    [Fact]
    public void Constructor_WithPageSizeEqualsMax_AcceptsValue()
    {
        // Act
        var pagination = new Pagination(1, Pagination.MaxPageSize);

        // Assert
        pagination.PageSize.Should().Be(Pagination.MaxPageSize);
    }

    [Fact]
    public void Skip_FirstPage_ReturnsZero()
    {
        // Arrange
        var pagination = new Pagination(1, 20);

        // Act & Assert
        pagination.Skip.Should().Be(0);
    }

    [Fact]
    public void Skip_SecondPage_CalculatesCorrectly()
    {
        // Arrange
        var pagination = new Pagination(2, 20);

        // Act & Assert
        pagination.Skip.Should().Be(20);
    }

    [Fact]
    public void Skip_ThirdPage_CalculatesCorrectly()
    {
        // Arrange
        var pagination = new Pagination(3, 25);

        // Act & Assert
        pagination.Skip.Should().Be(50);
    }

    [Fact]
    public void Skip_WithLargePageNumber_CalculatesCorrectly()
    {
        // Arrange
        var pagination = new Pagination(100, 20);

        // Act & Assert
        pagination.Skip.Should().Be(1980);
    }

    [Fact]
    public void DefaultPageSize_IsCorrectValue()
    {
        // Act & Assert
        Pagination.DefaultPageSize.Should().Be(20);
    }

    [Fact]
    public void MaxPageSize_IsCorrectValue()
    {
        // Act & Assert
        Pagination.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void Equality_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var pagination1 = new Pagination(2, 50);
        var pagination2 = new Pagination(2, 50);

        // Act & Assert
        (pagination1 == pagination2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentPageNumber_ReturnsFalse()
    {
        // Arrange
        var pagination1 = new Pagination(1, 50);
        var pagination2 = new Pagination(2, 50);

        // Act & Assert
        (pagination1 == pagination2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentPageSize_ReturnsFalse()
    {
        // Arrange
        var pagination1 = new Pagination(2, 50);
        var pagination2 = new Pagination(2, 75);

        // Act & Assert
        (pagination1 == pagination2).Should().BeFalse();
    }

    [Fact]
    public void Inequality_WithDifferentValues_ReturnsTrue()
    {
        // Arrange
        var pagination1 = new Pagination(1, 50);
        var pagination2 = new Pagination(2, 50);

        // Act & Assert
        (pagination1 != pagination2).Should().BeTrue();
    }

    [Fact]
    public void Inequality_WithSameValues_ReturnsFalse()
    {
        // Arrange
        var pagination1 = new Pagination(2, 50);
        var pagination2 = new Pagination(2, 50);

        // Act & Assert
        (pagination1 != pagination2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameValue()
    {
        // Arrange
        var pagination1 = new Pagination(2, 50);
        var pagination2 = new Pagination(2, 50);

        // Act & Assert
        pagination1.GetHashCode().Should().Be(pagination2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ReturnsDifferentValue()
    {
        // Arrange
        var pagination1 = new Pagination(1, 50);
        var pagination2 = new Pagination(2, 50);

        // Act & Assert
        pagination1.GetHashCode().Should().NotBe(pagination2.GetHashCode());
    }

    [Fact]
    public void CanBeUsedInSet()
    {
        // Arrange
        var pagination1 = new Pagination(2, 50);
        var pagination2 = new Pagination(2, 50);
        var set = new HashSet<Pagination> { pagination1 };

        // Act
        set.Add(pagination2);

        // Assert
        set.Should().HaveCount(1);
    }

    [Fact]
    public void CanBeUsedInDictionary()
    {
        // Arrange
        var pagination1 = new Pagination(1, 20);
        var pagination2 = new Pagination(2, 20);
        var dict = new Dictionary<Pagination, string> { { pagination1, "First" } };

        // Act
        dict.Add(pagination2, "Second");

        // Assert
        dict.Should().HaveCount(2);
        dict[pagination1].Should().Be("First");
    }

    [Fact]
    public void Skip_ConsistentWithPageCalculations()
    {
        // Arrange
        var items = Enumerable.Range(1, 100).ToList();
        var pageNumber = 3;
        var pageSize = 10;
        var pagination = new Pagination(pageNumber, pageSize);

        // Act
        var pagedItems = items.Skip(pagination.Skip).Take(pageSize).ToList();

        // Assert
        pagedItems.Should().HaveCount(10);
        pagedItems.First().Should().Be(21); // (3-1)*10 + 1
        pagedItems.Last().Should().Be(30);
    }

    [Fact]
    public void Constructor_WithInvalidInputs_AppliesAllClamping()
    {
        // Act
        var pagination = new Pagination(-5, -20);

        // Assert
        pagination.PageNumber.Should().Be(1);
        pagination.PageSize.Should().Be(Pagination.DefaultPageSize);
        pagination.Skip.Should().Be(0);
    }
}
