using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nac.Identity.Jwt;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Tests.Infrastructure;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

public class TenantSwitchServiceTests
{
    private static async Task<(TenantSwitchService svc, IdentityTestHost host, NacUser user)> BuildAsync()
    {
        var host = IdentityTestHost.Create(s =>
        {
            s.Configure<JwtOptions>(o =>
            {
                o.SecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters";
                o.Issuer = "TestIssuer";
                o.Audience = "TestAudience";
                o.ExpirationMinutes = 60;
            });
        });
        var user = new NacUser("switch@example.com", "Switch User") { IsHost = false };
        host.Db.Users.Add(user);
        await host.Db.SaveChangesAsync();

        var memberships = new MembershipService(host.Db, new RecordingPermissionGrantCache());
        var jwt = new JwtTokenService(host.GetRequiredService<IOptions<JwtOptions>>());
        var svc = new TenantSwitchService(
            host.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<NacUser>>(),
            memberships, jwt, host.GetRequiredService<IOptions<JwtOptions>>());

        return (svc, host, user);
    }

    [Fact]
    public async Task IssueTokenForTenantAsync_NoActiveMembership_Throws()
    {
        var (svc, _, user) = await BuildAsync();
        var act = async () => await svc.IssueTokenForTenantAsync(user.Id, "t1");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IssueTokenForTenantAsync_ActiveMembership_ReturnsToken()
    {
        var (svc, host, user) = await BuildAsync();
        var ms = new MembershipService(host.Db, new RecordingPermissionGrantCache());
        await ms.CreateActiveMembershipAsync(user.Id, "t1", [Guid.NewGuid()], isDefault: true);

        var result = await svc.IssueTokenForTenantAsync(user.Id, "t1");

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RoleIds.Should().HaveCount(1);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task IssueTokenForTenantAsync_InvitedMembership_Rejected()
    {
        var (svc, host, user) = await BuildAsync();
        var ms = new MembershipService(host.Db, new RecordingPermissionGrantCache());
        await ms.InviteAsync(user.Id, "t1", Guid.NewGuid(), [Guid.NewGuid()]);

        var act = async () => await svc.IssueTokenForTenantAsync(user.Id, "t1");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IssueTokenForTenantAsync_UnknownUser_Throws()
    {
        var (svc, _, _) = await BuildAsync();
        var act = async () => await svc.IssueTokenForTenantAsync(Guid.NewGuid(), "t1");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
