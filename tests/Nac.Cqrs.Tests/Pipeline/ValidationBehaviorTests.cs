using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Nac.Core.Results;
using Nac.Cqrs.Pipeline;
using Nac.Cqrs.Tests.Helpers;
using NSubstitute;
using Xunit;

namespace Nac.Cqrs.Tests.Pipeline;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleAsync_NoValidators_CallsNext()
    {
        // Arrange
        var validators = Array.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Test");
        var nextResult = "next called";
        var nextCalled = false;

        RequestHandlerDelegate<string> next = async () =>
        {
            nextCalled = true;
            return await ValueTask.FromResult(nextResult).ConfigureAwait(false);
        };

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        result.Should().Be(nextResult);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CallsNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(
            Arg.Any<ValidationContext<TestCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Test");
        var nextResult = "next called";
        var nextCalled = false;

        RequestHandlerDelegate<string> next = async () =>
        {
            nextCalled = true;
            return await ValueTask.FromResult(nextResult).ConfigureAwait(false);
        };

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        result.Should().Be(nextResult);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ResultType_ReturnsResultInvalid()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
            new("Email", "Invalid email format"),
        };

        var validator = Substitute.For<IValidator<CreateUserCommand>>();
        validator.ValidateAsync(
            Arg.Any<ValidationContext<CreateUserCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<CreateUserCommand, Result<string>>(validators);
        var command = new CreateUserCommand("", "");

        RequestHandlerDelegate<Result<string>> next = async () =>
            await ValueTask.FromResult(Result<string>.Success("should-not-reach")).ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().HaveCount(2);
        result.ValidationErrors[0].Identifier.Should().Be("Name");
        result.ValidationErrors[0].ErrorMessage.Should().Be("Name is required");
        result.ValidationErrors[1].Identifier.Should().Be("Email");
        result.ValidationErrors[1].ErrorMessage.Should().Be("Invalid email format");
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_NonResultType_ThrowsValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
        };

        var validator = Substitute.For<IValidator<TestCommand>>();
        validator.ValidateAsync(
            Arg.Any<ValidationContext<TestCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("should-not-reach").ConfigureAwait(false);

        // Act
        var act = async () => await behavior.HandleAsync(command, next);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_MultipleValidators_CollectsAllFailures()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestCommand>>();
        validator1.ValidateAsync(
            Arg.Any<ValidationContext<TestCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Error 1") }));

        var validator2 = Substitute.For<IValidator<TestCommand>>();
        validator2.ValidateAsync(
            Arg.Any<ValidationContext<TestCommand>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Email", "Error 2") }));

        var validators = new[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("should-not-reach").ConfigureAwait(false);

        // Act
        var act = async () => await behavior.HandleAsync(command, next);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
