using FluentAssertions;
using Nac.Core.Results;
using Xunit;

namespace Nac.Core.Tests.Results;

public class ResultTTests
{
    [Fact]
    public void Success_WithValue_ReturnsSuccessResult()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        result.Status.Should().Be(ResultStatus.Ok);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Errors.Should().BeEmpty();
        result.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        // Arrange
        var value = "test";

        // Act
        Result<string> result = value;

        // Assert
        result.Status.Should().Be(ResultStatus.Ok);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void NotFound_WithoutMessage_ReturnsNotFoundResult()
    {
        // Act
        var result = Result<int>.NotFound();

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NotFound_WithMessage_ReturnsNotFoundResultWithMessage()
    {
        // Arrange
        var message = "Item not found";

        // Act
        var result = Result<string>.NotFound(message);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
        result.Errors.Should().ContainSingle().Which.Should().Be(message);
    }

    [Fact]
    public void Invalid_WithValidationErrors_ReturnsInvalidResult()
    {
        // Arrange
        var error1 = new ValidationError("Field1", "Error message 1");
        var error2 = new ValidationError("Field2", "Error message 2");

        // Act
        var result = Result<int>.Invalid(error1, error2);

        // Assert
        result.Status.Should().Be(ResultStatus.Invalid);
        result.IsSuccess.Should().BeFalse();
        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
        result.ValidationErrors.Should().HaveCount(2);
    }

    [Fact]
    public void Forbidden_WithoutMessage_ReturnsForbiddenResult()
    {
        // Act
        var result = Result<int>.Forbidden();

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.IsSuccess.Should().BeFalse();
        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Forbidden_WithMessage_ReturnsForbiddenResultWithMessage()
    {
        // Arrange
        var message = "You do not have permission";

        // Act
        var result = Result<int>.Forbidden(message);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(message);
    }

    [Fact]
    public void Conflict_WithoutMessage_ReturnsConflictResult()
    {
        // Act
        var result = Result<int>.Conflict();

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Conflict_WithMessage_ReturnsConflictResultWithMessage()
    {
        // Arrange
        var message = "Resource conflict";

        // Act
        var result = Result<int>.Conflict(message);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(message);
    }

    [Fact]
    public void Error_WithErrorMessages_ReturnsErrorResult()
    {
        // Arrange
        var error1 = "Error 1";
        var error2 = "Error 2";

        // Act
        var result = Result<int>.Error(error1, error2);

        // Assert
        result.Status.Should().Be(ResultStatus.Error);
        result.IsSuccess.Should().BeFalse();
        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Map_OnSuccessResult_TransformsValue()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Status.Should().Be(ResultStatus.Ok);
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnFailedResult_PropagatesError()
    {
        // Arrange
        var result = Result<int>.NotFound("Not found");

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeFalse();
        mapped.Status.Should().Be(ResultStatus.NotFound);
        mapped.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
        mapped.Errors.Should().ContainSingle().Which.Should().Be("Not found");
    }

    [Fact]
    public void Map_WithTypeConversion_SucceedsOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public void Map_WithComplexTransformation_PreservesErrorState()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result<int>.Error(errors);

        // Act
        var mapped = result.Map(x => new { Value = x, Doubled = x * 2 });

        // Assert
        mapped.IsSuccess.Should().BeFalse();
        mapped.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void InvalidResult_PreservesValidationErrors_OnMap()
    {
        // Arrange
        var error = new ValidationError("Field", "Message");
        var result = Result<int>.Invalid(error);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.Should().BeFalse();
        mapped.ValidationErrors.Should().HaveCount(1);
        mapped.ValidationErrors.First().Should().Be(error);
    }
}
