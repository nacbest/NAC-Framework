namespace Nac.Core.Abstractions.Identity;

public interface IIdentityService
{
    Task<UserInfo?> GetUserInfoAsync(Guid userId);
    Task<IReadOnlyList<UserInfo>> GetUsersAsync(IEnumerable<Guid> userIds);
    Task<bool> IsInRoleAsync(Guid userId, string role);
}
