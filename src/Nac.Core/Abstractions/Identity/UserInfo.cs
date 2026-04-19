namespace Nac.Core.Abstractions.Identity;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string? FullName,
    string TenantId,
    IReadOnlyList<string> Roles);
