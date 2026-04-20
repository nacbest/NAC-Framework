using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Core.Domain;
using Nac.Identity.Permissions.Host;
using Nac.Identity.Persistence;
using Nac.Identity.Tests.Infrastructure;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Persistence;

/// <summary>
/// R4 guard: <see cref="HostQueryExtensions.AsHostQueryAsync"/> requires BOTH
/// <c>IsHost</c> AND the <c>Host.AccessAllTenants</c> permission. Either one missing
/// → <see cref="ForbiddenAccessException"/>. Only host+perm gets the unfiltered query.
/// </summary>
public class HostQueryExtensionsTests
{
    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Email { get; set; } = "u@e.com";
        public string? Name { get; set; }
        public string? TenantId { get; set; }
        public IReadOnlyList<Guid> RoleIds { get; set; } = [];
        public IReadOnlyList<string> Roles { get; set; } = [];
        public bool IsAuthenticated { get; set; } = true;
        public bool IsHost { get; set; }
        public Guid? ImpersonatorId { get; set; }
    }

    [Fact]
    public async Task AsHostQueryAsync_NonHostUser_Throws()
    {
        var user = new StubCurrentUser { IsHost = false };
        var checker = Substitute.For<IPermissionChecker>();
        var host = IdentityTestHost.Create();

        var act = async () => await host.Db.Users.AsHostQueryAsync(user, checker);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task AsHostQueryAsync_HostWithoutPermission_Throws()
    {
        var user = new StubCurrentUser { IsHost = true };
        var checker = Substitute.For<IPermissionChecker>();
        checker.IsGrantedAsync(HostPermissions.AccessAllTenants, Arg.Any<CancellationToken>())
            .Returns(false);
        var host = IdentityTestHost.Create();

        var act = async () => await host.Db.Users.AsHostQueryAsync(user, checker);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task AsHostQueryAsync_HostWithPermission_ReturnsUnfilteredQueryable()
    {
        var user = new StubCurrentUser { IsHost = true };
        var checker = Substitute.For<IPermissionChecker>();
        checker.IsGrantedAsync(HostPermissions.AccessAllTenants, Arg.Any<CancellationToken>())
            .Returns(true);
        var host = IdentityTestHost.Create();
        host.Db.Users.Add(new NacUser("soft@example.com") { IsDeleted = true });
        await host.Db.SaveChangesAsync();

        var query = await host.Db.Users.AsHostQueryAsync(user, checker);

        // Normal query filter excludes soft-deleted; host bypass returns the row.
        var bypassCount = query.Count();
        bypassCount.Should().Be(1, "AsHostQueryAsync must apply IgnoreQueryFilters()");
    }
}
