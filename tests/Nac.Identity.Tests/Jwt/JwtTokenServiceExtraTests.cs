using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

/// <summary>
/// Pattern-A JWT guarantees:
/// - role_ids claim round-trips a JSON array of Guids
/// - token size stays well under the 2KB contract for realistic role counts
/// - no permission claims are embedded
/// - tenantless tokens carry neither tenant_id nor role_ids claims
/// </summary>
public class JwtTokenServiceExtraTests
{
    private static JwtTokenService CreateService() =>
        new(Options.Create(new JwtOptions
        {
            SecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60,
        }));

    [Fact]
    public void GenerateToken_WithRoleIds_EmbedsJsonArrayClaim()
    {
        var svc = CreateService();
        var roles = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var token = svc.GenerateToken(Guid.NewGuid(), "t1", "u@e.com", null, roles, false);
        var claim = Decode(token).Single(c => c.Type == NacIdentityClaims.RoleIds);

        JsonSerializer.Deserialize<Guid[]>(claim.Value).Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void GenerateToken_Tenantless_HasNoRoleIdsClaim()
    {
        var svc = CreateService();
        var token = svc.GenerateToken(Guid.NewGuid(), null, "u@e.com", null, [], false);

        Decode(token).Should().NotContain(c => c.Type == NacIdentityClaims.RoleIds);
    }

    [Fact]
    public void GenerateToken_NeverEmbedsPermissionClaim()
    {
        var svc = CreateService();
        var token = svc.GenerateToken(Guid.NewGuid(), "t1", "u@e.com", null,
            [Guid.NewGuid(), Guid.NewGuid()], false);

        Decode(token).Should().NotContain(c => c.Type == "permission");
    }

    [Fact]
    public void GenerateToken_WithFiveRoles_StaysUnder2KB()
    {
        var svc = CreateService();
        var roles = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        var token = svc.GenerateToken(Guid.NewGuid(), "tenant-xyz",
            "user@example.com", "Jane Doe", roles, isHost: false);

        token.Length.Should().BeLessThan(2048,
            "Pattern A JWT size budget is <2KB to stay below typical header limits");
    }

    [Fact]
    public void GenerateToken_HostUser_HasIsHostClaim()
    {
        var svc = CreateService();
        var token = svc.GenerateToken(Guid.NewGuid(), null, "host@platform.local", null, [], true);

        Decode(token).Should().ContainSingle(c =>
            c.Type == NacIdentityClaims.IsHost && c.Value == "true");
    }

    private static List<System.Security.Claims.Claim> Decode(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;
        return jwt!.Claims.ToList();
    }
}
