using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Nac.Core.Results;

namespace Nac.Testing.Extensions;

public static class ResultAssertionExtensions
{
    public static ResultAssertions Should(this Result result) =>
        new(result, AssertionChain.GetOrCreate());
}

public class ResultAssertions(Result subject, AssertionChain assertionChain)
    : ReferenceTypeAssertions<Result, ResultAssertions>(subject, assertionChain)
{
    protected override string Identifier => "result";

    public AndConstraint<ResultAssertions> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Subject.IsSuccess.Should().BeTrue(because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeNotFound(string because = "", params object[] becauseArgs)
    {
        Subject.Status.Should().Be(ResultStatus.NotFound, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeInvalid(string because = "", params object[] becauseArgs)
    {
        Subject.Status.Should().Be(ResultStatus.Invalid, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeForbidden(string because = "", params object[] becauseArgs)
    {
        Subject.Status.Should().Be(ResultStatus.Forbidden, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeConflict(string because = "", params object[] becauseArgs)
    {
        Subject.Status.Should().Be(ResultStatus.Conflict, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeError(string because = "", params object[] becauseArgs)
    {
        Subject.Status.Should().Be(ResultStatus.Error, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> HaveStatus(ResultStatus expected,
        string because = "", params object[] becauseArgs)
    {
        Subject.Status.Should().Be(expected, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> HaveError(string expectedError,
        string because = "", params object[] becauseArgs)
    {
        Subject.Errors.Should().Contain(expectedError, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }
}
