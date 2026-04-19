using Microsoft.AspNetCore.Identity;
using Nac.Core.Abstractions.Identity;
using Nac.Identity.Users;

namespace Nac.Identity.Services;

/// <summary>
/// Implements <see cref="IIdentityService"/> by delegating to
/// <see cref="UserManager{NacUser}"/>. Registered as scoped.
/// </summary>
internal sealed class IdentityService(UserManager<NacUser> userManager) : IIdentityService
{
    /// <inheritdoc/>
    public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return new UserInfo(user.Id, user.Email ?? string.Empty, user.FullName, user.TenantId, roles.ToList());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserInfo>> GetUsersAsync(IEnumerable<Guid> userIds)
    {
        var idSet = userIds.ToHashSet();
        var users = userManager.Users.Where(u => idSet.Contains(u.Id)).ToList();

        var results = new List<UserInfo>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            results.Add(new UserInfo(user.Id, user.Email ?? string.Empty, user.FullName, user.TenantId, roles.ToList()));
        }
        return results;
    }

    /// <inheritdoc/>
    public async Task<bool> IsInRoleAsync(Guid userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, role);
    }
}
