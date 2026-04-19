using FluentAssertions;
using Nac.Core.Results;
using Xunit;

namespace Nac.Core.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsResultWithOkStatus()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.Status.Should().Be(ResultStatus.Ok);
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void NotFound_WithoutMessage_ReturnsNotFoundStatus()
    {
        // Act
        var result = Result.NotFound();

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NotFound_WithMessage_ReturnsNotFoundStatusWithMessage()
    {
        // Arrange
        var message = "Resource not found";

        // Act
        var result = Result.NotFound(message);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(message);
    }

    [Fact]
    public void Invalid_WithValidationErrors_ReturnsInvalidStatus()
    {
        // Arrange
        var error1 = new ValidationError("Name", "Name is required");
        var error2 = new ValidationError("Email", "Email is invalid");

        // Act
        var result = Result.Invalid(error1, error2);

        // Assert
        result.Status.Should().Be(ResultStatus.Invalid);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().HaveCount(2);
        result.ValidationErrors.Should().Contain(error1);
        result.ValidationErrors.Should().Contain(error2);
    }

    [Fact]
    public void Forbidden_WithoutMessage_ReturnsForbiddenStatus()
    {
        // Act
        var result = Result.Forbidden();

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Forbidden_WithMessage_ReturnsForbiddenStatusWithMessage()
    {
        // Arrange
        var message = "Access denied";

        // Act
        var result = Result.Forbidden(message);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(message);
    }

    [Fact]
    public void Conflict_WithoutMessage_ReturnsConflictStatus()
    {
        // Act
        var result = Result.Conflict();

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Conflict_WithMessage_ReturnsConflictStatusWithMessage()
    {
        // Arrange
        var message = "Resource already exists";

        // Act
        var result = Result.Conflict(message);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(message);
    }

    [Fact]
    public void Error_WithErrorMessages_ReturnsErrorStatus()
    {
        // Arrange
        var error1 = "Error 1";
        var error2 = "Error 2";

        // Act
        var result = Result.Error(error1, error2);

        // Assert
        result.Status.Should().Be(ResultStatus.Error);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(error1);
        result.Errors.Should().Contain(error2);
    }

    [Fact]
    public void Error_WithoutErrors_ReturnsErrorStatusWithoutMessages()
    {
        // Act
        var result = Result.Error();

        // Assert
        result.Status.Should().Be(ResultStatus.Error);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Success_WithValue_CreatesResultOfT()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result.Success(value);

        // Assert
        result.Should().BeOfType<Result<int>>();
        result.IsSuccess.Should().BeTrue();
    }
}
