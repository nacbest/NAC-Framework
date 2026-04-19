using FluentAssertions;
using Nac.Core.Results;
using Nac.Testing.Extensions;
using Xunit;
using Xunit.Sdk;

namespace Nac.Testing.Tests.Extensions;

public class ResultAssertionExtensionsTests
{
    [Fact]
    public void BeSuccess_OnSuccess_Passes()
    {
        var result = Result.Success();

        Action act = () => result.Should().BeSuccess();

        act.Should().NotThrow();
    }

    [Fact]
    public void BeSuccess_OnError_ThrowsAssertionException()
    {
        var result = Result.Error("something failed");

        Action act = () => result.Should().BeSuccess();

        // FluentAssertions + xUnit v3 raises XunitException on assertion failure
        act.Should().Throw<XunitException>();
    }

    [Fact]
    public void BeNotFound_OnNotFound_Passes()
    {
        var result = Result.NotFound("item missing");

        Action act = () => result.Should().BeNotFound();

        act.Should().NotThrow();
    }

    [Fact]
    public void BeNotFound_OnSuccess_ThrowsAssertionException()
    {
        var result = Result.Success();

        Action act = () => result.Should().BeNotFound();

        act.Should().Throw<XunitException>();
    }

    [Fact]
    public void HaveStatus_MatchesCorrectly()
    {
        var result = Result.Forbidden("no access");

        Action act = () => result.Should().HaveStatus(ResultStatus.Forbidden);

        act.Should().NotThrow();
    }

    [Fact]
    public void HaveError_ContainsMessage()
    {
        var result = Result.Error("disk full", "retry later");

        Action act = () => result.Should().HaveError("disk full");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeInvalid_OnInvalid_Passes()
    {
        var result = Result.Invalid(new ValidationError("Name", "Name is required"));

        Action act = () => result.Should().BeInvalid();

        act.Should().NotThrow();
    }

    [Fact]
    public void BeInvalid_OnSuccess_ThrowsAssertionException()
    {
        var result = Result.Success();

        Action act = () => result.Should().BeInvalid();

        act.Should().Throw<XunitException>();
    }
}
