namespace Nac.Core.Results;

public class Result
{
    public ResultStatus Status { get; }
    public bool IsSuccess => Status == ResultStatus.Ok;
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    protected Result(
        ResultStatus status,
        IReadOnlyList<string>? errors = null,
        IReadOnlyList<ValidationError>? validationErrors = null)
    {
        Status = status;
        Errors = errors ?? [];
        ValidationErrors = validationErrors ?? [];
    }

    public static Result Success() => new(ResultStatus.Ok);

    public static Result NotFound(string? message = null) =>
        new(ResultStatus.NotFound, message is null ? [] : [message]);

    public static Result Invalid(params ValidationError[] errors) =>
        new(ResultStatus.Invalid, validationErrors: errors);

    public static Result Forbidden(string? message = null) =>
        new(ResultStatus.Forbidden, message is null ? [] : [message]);

    public static Result Conflict(string? message = null) =>
        new(ResultStatus.Conflict, message is null ? [] : [message]);

    public static Result Error(params string[] errors) =>
        new(ResultStatus.Error, errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
}
