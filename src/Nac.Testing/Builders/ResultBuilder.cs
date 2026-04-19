using Nac.Core.Results;

namespace Nac.Testing.Builders;

/// <summary>
/// Convenience factory for creating Result instances in tests.
/// </summary>
public static class ResultBuilder
{
    public static Result Success() => Result.Success();
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result NotFound(string? message = null) => Result.NotFound(message);
    public static Result<T> NotFound<T>(string? message = null) => Result<T>.NotFound(message);

    public static Result Invalid(params ValidationError[] errors) => Result.Invalid(errors);
    public static Result<T> Invalid<T>(params ValidationError[] errors) => Result<T>.Invalid(errors);

    public static Result Forbidden(string? message = null) => Result.Forbidden(message);
    public static Result<T> Forbidden<T>(string? message = null) => Result<T>.Forbidden(message);

    public static Result Error(params string[] errors) => Result.Error(errors);
    public static Result<T> Error<T>(params string[] errors) => Result<T>.Error(errors);

    public static Result Conflict(string? message = null) => Result.Conflict(message);
}
