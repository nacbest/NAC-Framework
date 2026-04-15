using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nac.Abstractions.Auth;
using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// Implements <see cref="IIdentityService"/> using ASP.NET Core Identity.
/// Provides user lookups for business modules without coupling them to Identity infrastructure.
/// </summary>
internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<NacUser> _userManager;

    public IdentityService(UserManager<NacUser> userManager)
        => _userManager = userManager;

    public async Task<UserInfo?> GetUserInfoAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserInfo(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList());
    }

    public async Task<IReadOnlyList<UserInfo>> GetUsersAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.ToHashSet();
        if (ids.Count == 0)
            return [];

        var users = await _userManager.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);

        var result = new List<UserInfo>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserInfo(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList()));
        }
        return result;
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;
        return await _userManager.IsInRoleAsync(user, role);
    }
}
