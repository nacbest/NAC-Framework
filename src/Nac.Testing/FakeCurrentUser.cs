using Nac.Core.Auth;

namespace Nac.Testing;

/// <summary>
/// Configurable <see cref="ICurrentUser"/> for tests. Supports permission checks
/// with wildcard matching (e.g., <c>orders.*</c> matches <c>orders.create</c>).
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    private readonly HashSet<string> _permissions;

    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public bool IsAuthenticated { get; set; }
    public IReadOnlySet<string> Permissions => _permissions;

    public FakeCurrentUser(
        string? userId = null,
        string? userName = null,
        bool isAuthenticated = false,
        IEnumerable<string>? permissions = null)
    {
        UserId = userId;
        UserName = userName;
        IsAuthenticated = isAuthenticated;
        _permissions = permissions is not null ? [..permissions] : [];
    }

    /// <summary>
    /// Creates an authenticated user with the given permissions.
    /// </summary>
    public static FakeCurrentUser Authenticated(
        string userId = "test-user",
        params string[] permissions)
        => new(userId, userId, true, permissions);

    /// <summary>Creates an unauthenticated (anonymous) user.</summary>
    public static FakeCurrentUser Anonymous() => new();

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return false;

        if (_permissions.Contains(permission))
            return true;

        // Wildcard matching — mirrors JwtCurrentUser.MatchesWildcard
        foreach (var p in _permissions)
        {
            if (p == "*")
                return true;

            // "orders.*" matches "orders.create"
            if (p.EndsWith(".*"))
            {
                var prefix = p[..^1]; // "orders."
                if (permission.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }

            // "*.create" matches "orders.create"
            if (p.StartsWith("*."))
            {
                var suffix = p[1..]; // ".create"
                if (permission.EndsWith(suffix, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Adds a permission at runtime during the test.</summary>
    public void Grant(string permission) => _permissions.Add(permission);

    /// <summary>Removes a permission at runtime during the test.</summary>
    public void Revoke(string permission) => _permissions.Remove(permission);
}
