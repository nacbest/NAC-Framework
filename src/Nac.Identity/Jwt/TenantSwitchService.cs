using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Nac.Identity.Memberships;
using Nac.Identity.Users;

namespace Nac.Identity.Jwt;

/// <summary>
/// Validates the caller's Active membership in the target tenant, resolves role ids,
/// and issues a fresh tenant-scoped JWT via <see cref="JwtTokenService"/>.
/// </summary>
internal sealed class TenantSwitchService(
    UserManager<NacUser> userManager,
    IMembershipService memberships,
    JwtTokenService jwt,
    IOptions<JwtOptions> jwtOptions) : ITenantSwitchService
{
    public async Task<TenantSwitchResult> IssueTokenForTenantAsync(Guid userId, string tenantId,
                                                                  CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var roleIds = await memberships.GetRoleIdsAsync(userId, tenantId, ct);
        if (roleIds.Count == 0)
        {
            // Either no membership or not Active — treat as unauthorized.
            throw new InvalidOperationException(
                $"User {userId} has no active membership in tenant '{tenantId}'.");
        }

        var token = jwt.GenerateToken(
            userId: user.Id,
            tenantId: tenantId,
            email: user.Email ?? string.Empty,
            name: user.FullName,
            roleIds: roleIds,
            isHost: user.IsHost);

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        return new TenantSwitchResult(token, roleIds, expiresAt);
    }
}
