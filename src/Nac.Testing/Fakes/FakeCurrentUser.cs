using Nac.Core.Abstractions.Identity;

namespace Nac.Testing.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; } = "test@example.com";
    public string TenantId { get; set; } = "default";
    public IReadOnlyList<string> Roles { get; set; } = ["User"];
    public bool IsAuthenticated { get; set; } = true;

    public static FakeCurrentUser Anonymous() => new()
    {
        Id = Guid.Empty, Email = null, IsAuthenticated = false, Roles = []
    };

    public static FakeCurrentUser Admin() => new()
    {
        Roles = ["Admin"], Email = "admin@example.com"
    };

    public static FakeCurrentUser Create(Guid id, string email, params string[] roles) => new()
    {
        Id = id, Email = email, Roles = roles
    };
}
