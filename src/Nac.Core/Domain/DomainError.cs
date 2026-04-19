namespace Nac.Core.Domain;

public sealed record DomainError(string Code, string Message)
{
    public static DomainError NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");

    public static DomainError Validation(string code, string message) =>
        new(code, message);

    public static DomainError Conflict(string code, string message) =>
        new(code, message);

    public static DomainError Unauthorized(string code, string message) =>
        new(code, message);
}
