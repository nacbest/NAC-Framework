namespace Nac.Core.Domain;

/// <summary>
/// Thrown when an authenticated caller lacks the required permission.
/// Maps to HTTP 403 Forbidden (distinct from 401 Unauthorized which means unauthenticated).
/// </summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);
