namespace Nac.Core.Abstractions.Identity;

public sealed record UserInfo(
    Guid Id,
    string Email,
    string? FullName,
    IReadOnlyList<string> Roles);
