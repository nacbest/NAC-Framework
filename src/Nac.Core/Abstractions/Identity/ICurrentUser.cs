namespace Nac.Core.Abstractions.Identity;

public interface ICurrentUser
{
    Guid Id { get; }
    string? Email { get; }
    string TenantId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
