namespace Nac.Core;

/// <summary>
/// Typed domain error for business rule violations.
/// Use static factory methods for common error patterns.
/// </summary>
public sealed record DomainError(string Code, string Message)
{
    public static DomainError NotFound(string entity, object id)
        => new($"{entity}.not_found", $"{entity} with id '{id}' was not found.");

    public static DomainError Validation(string code, string message)
        => new(code, message);

    public static DomainError Conflict(string code, string message)
        => new(code, message);
}
