using FluentAssertions;
using Nac.Core.Results;
using Nac.Testing.Builders;
using Xunit;

namespace Nac.Testing.Tests.Builders;

public class ResultBuilderTests
{
    [Fact]
    public void Success_IsSuccess()
    {
        var result = ResultBuilder.Success();

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Ok);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void NotFound_HasNotFoundStatus()
    {
        var result = ResultBuilder.NotFound("item not found");

        result.Status.Should().Be(ResultStatus.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("item not found");
    }

    [Fact]
    public void Invalid_HasValidationErrors()
    {
        var error = new ValidationError("Email", "Email is invalid");
        var result = ResultBuilder.Invalid(error);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainSingle(e =>
            e.Identifier == "Email" && e.ErrorMessage == "Email is invalid");
    }

    [Fact]
    public void Error_HasErrorMessages()
    {
        var result = ResultBuilder.Error("Something went wrong", "Try again");

        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Something went wrong");
        result.Errors.Should().Contain("Try again");
    }

    [Fact]
    public void SuccessT_HasValue()
    {
        var result = ResultBuilder.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Status.Should().Be(ResultStatus.Ok);
    }

    [Fact]
    public void Forbidden_HasForbiddenStatus()
    {
        var result = ResultBuilder.Forbidden("access denied");

        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Errors.Should().Contain("access denied");
    }

    [Fact]
    public void Conflict_HasConflictStatus()
    {
        var result = ResultBuilder.Conflict("already exists");

        result.Status.Should().Be(ResultStatus.Conflict);
        result.IsSuccess.Should().BeFalse();
    }
}
