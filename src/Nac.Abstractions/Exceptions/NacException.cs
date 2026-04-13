namespace Nac.Abstractions.Exceptions;

/// <summary>Base exception for all NAC framework exceptions.</summary>
public abstract class NacException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>HTTP status code this exception maps to.</summary>
    public abstract int StatusCode { get; }
}

/// <summary>Thrown when input validation fails. Maps to 400 Bad Request.</summary>
public sealed class NacValidationException : NacException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public override int StatusCode => 400;

    public NacValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
        => Errors = errors;

    public NacValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] }) { }
}

/// <summary>Thrown when authentication is required but missing or invalid. Maps to 401.</summary>
public sealed class NacUnauthorizedException(string message = "Authentication is required.")
    : NacException(message)
{
    public override int StatusCode => 401;
}

/// <summary>Thrown when the user lacks required permissions. Maps to 403.</summary>
public sealed class NacForbiddenException(string message = "You do not have permission to perform this action.")
    : NacException(message)
{
    public override int StatusCode => 403;
}

/// <summary>Thrown when a requested resource is not found. Maps to 404.</summary>
public sealed class NacNotFoundException : NacException
{
    public override int StatusCode => 404;

    public NacNotFoundException(string message) : base(message) { }

    public NacNotFoundException(string entityName, object id)
        : base($"{entityName} with ID '{id}' was not found.") { }
}

/// <summary>Thrown when an operation conflicts with current state. Maps to 409.</summary>
public sealed class NacConflictException(string message) : NacException(message)
{
    public override int StatusCode => 409;
}

/// <summary>Thrown when a domain rule is violated. Maps to 422 Unprocessable Entity.</summary>
public sealed class NacDomainException(string message) : NacException(message)
{
    public override int StatusCode => 422;
}
