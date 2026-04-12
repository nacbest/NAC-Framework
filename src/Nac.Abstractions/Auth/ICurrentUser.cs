namespace Nac.Auth;

/// <summary>
/// Provides information about the currently authenticated user within the request scope.
/// Injected as a scoped service. Returns null/empty values for unauthenticated requests.
/// </summary>
public interface ICurrentUser
{
    /// <summary>User's unique identifier. Null if not authenticated.</summary>
    string? UserId { get; }

    /// <summary>User's display name.</summary>
    string? UserName { get; }

    /// <summary>Whether the current request is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Flat set of permission strings assigned to this user (for current tenant if multi-tenant).</summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>Checks whether the user holds a specific permission. Supports wildcard matching.</summary>
    bool HasPermission(string permission);
}
