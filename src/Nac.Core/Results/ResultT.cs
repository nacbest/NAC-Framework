namespace Nac.Core.Results;

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value on a failed Result. Check IsSuccess first.");

    internal Result(
        T? value,
        ResultStatus status,
        IReadOnlyList<string>? errors = null,
        IReadOnlyList<ValidationError>? validationErrors = null)
        : base(status, errors, validationErrors)
    {
        _value = value;
    }

    public static Result<T> Success(T value) => new(value, ResultStatus.Ok);

    public new static Result<T> NotFound(string? message = null) =>
        new(default, ResultStatus.NotFound, message is null ? [] : [message]);

    public new static Result<T> Invalid(params ValidationError[] errors) =>
        new(default, ResultStatus.Invalid, validationErrors: errors);

    public new static Result<T> Forbidden(string? message = null) =>
        new(default, ResultStatus.Forbidden, message is null ? [] : [message]);

    public new static Result<T> Conflict(string? message = null) =>
        new(default, ResultStatus.Conflict, message is null ? [] : [message]);

    public new static Result<T> Error(params string[] errors) =>
        new(default, ResultStatus.Error, errors);

    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Transforms the value if successful; propagates errors otherwise.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess
            ? Result<TOut>.Success(mapper(Value!))
            : new Result<TOut>(default, Status, Errors, ValidationErrors);

}
