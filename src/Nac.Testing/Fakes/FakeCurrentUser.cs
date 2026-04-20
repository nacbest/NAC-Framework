using Nac.Core.Abstractions.Identity;

namespace Nac.Testing.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; } = "test@example.com";
    public string? Name { get; set; }
    public string? TenantId { get; set; } = "default";

    /// <summary>Role GUIDs (Pattern A: role_ids JWT claim).</summary>
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];

    /// <summary>String role names — test convenience; not part of ICurrentUser.</summary>
    public IReadOnlyList<string> Roles { get; set; } = ["User"];

    public bool IsAuthenticated { get; set; } = true;
    public bool IsHost { get; set; }
    public Guid? ImpersonatorId { get; set; }

    public static FakeCurrentUser Anonymous() => new()
    {
        Id = Guid.Empty, Email = null, IsAuthenticated = false, Roles = [], TenantId = null
    };

    public static FakeCurrentUser Admin() => new()
    {
        Roles = ["Admin"], Email = "admin@example.com"
    };

    public static FakeCurrentUser Create(Guid id, string email, params string[] roles) => new()
    {
        Id = id, Email = email, Roles = roles
    };

    public static FakeCurrentUser Host(string email = "host@platform.local") => new()
    {
        Email = email, IsHost = true, TenantId = null
    };
}
