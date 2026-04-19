using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

/// <summary>
/// Composite and integration tests for JwtTokenService with multiple claims.
/// Tests token generation with complex claim combinations.
/// </summary>
public class JwtTokenServiceCompositeTests
{
    private const string TestSecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters-long";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";
    private const int TestExpirationMinutes = 60;

    private readonly JwtOptions _jwtOptions = new()
    {
        SecretKey = TestSecretKey,
        Issuer = TestIssuer,
        Audience = TestAudience,
        ExpirationMinutes = TestExpirationMinutes,
    };

    [Fact]
    public async Task GenerateTokenAsync_WithMultipleRolesAndPermissions_TokenIncludesAllClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new[] { "Admin", "Moderator", "User" };
        var permissionClaims = new[]
        {
            new Claim(NacIdentityClaims.Permission, "Users.Create"),
            new Claim(NacIdentityClaims.Permission, "Users.Read"),
            new Claim(NacIdentityClaims.Permission, "Users.Update"),
            new Claim(NacIdentityClaims.Permission, "Users.Delete"),
        };
        var userManager = CreateMockUserManager(user, roles, permissionClaims);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var claims = DecodeToken(token);

        // Assert
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(3);
        var permClaims = claims.Where(c => c.Type == NacIdentityClaims.Permission).ToList();
        permClaims.Should().HaveCount(4);
    }

    [Fact]
    public async Task GenerateTokenAsync_TokenProperties_AllRequiredPresent()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new[] { "Admin" };
        var permissionClaims = new[] { new Claim(NacIdentityClaims.Permission, "Test.Read") };
        var userManager = CreateMockUserManager(user, roles, permissionClaims);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        jwtToken!.Should().NotBeNull();
        jwtToken.Issuer.Should().Be(TestIssuer);
        jwtToken.Audiences.Should().Contain(TestAudience);
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwtToken.Claims.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateTokenAsync_WithUserRoles_ProducedTokenContainsRoleClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new[] { "Admin", "Manager" };
        var userManager = CreateMockUserManager(user, roles, claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var claims = DecodeToken(token);

        // Assert
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(2);
        roleClaims.Select(c => c.Value).Should().Contain(roles);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithUserPermissions_ProducedTokenContainsPermissionClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var permissionClaims = new[]
        {
            new Claim(NacIdentityClaims.Permission, "Users.Create"),
            new Claim(NacIdentityClaims.Permission, "Users.Delete"),
        };
        var userManager = CreateMockUserManager(user, roles: [], claims: permissionClaims);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var claims = DecodeToken(token);

        // Assert
        var permClaims = claims.Where(c => c.Type == NacIdentityClaims.Permission).ToList();
        permClaims.Should().HaveCount(2);
        permClaims.Select(c => c.Value).Should().Contain(new[] { "Users.Create", "Users.Delete" });
    }

    [Fact]
    public async Task GenerateTokenAsync_WithNonPermissionClaims_TokenDoesNotIncludeNonPermissionClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var claims = new[]
        {
            new Claim("custom_claim", "custom_value"),
            new Claim(NacIdentityClaims.Permission, "Users.Read"),
        };
        var userManager = CreateMockUserManager(user, roles: [], claims);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var tokenClaims = DecodeToken(token);

        // Assert
        var customClaims = tokenClaims.Where(c => c.Type == "custom_claim").ToList();
        customClaims.Should().BeEmpty();
        var permClaims = tokenClaims.Where(c => c.Type == NacIdentityClaims.Permission).ToList();
        permClaims.Should().HaveCount(1);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static NacUser CreateTestUser()
    {
        return new NacUser("user@example.com", "tenant-123");
    }

    private static UserManager<NacUser> CreateMockUserManager(
        NacUser user,
        string[] roles,
        Claim[] claims)
    {
        var userStore = Substitute.For<IUserStore<NacUser>>();
        var userManager = Substitute.For<UserManager<NacUser>>(
            userStore, null, null, null, null, null, null, null, null);

        userManager.GetRolesAsync(user).Returns(Task.FromResult<IList<string>>(roles.ToList()));
        userManager.GetClaimsAsync(user).Returns(Task.FromResult<IList<Claim>>(claims.ToList()));

        return userManager;
    }

    private static List<Claim> DecodeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
        return jwtToken!.Claims.ToList();
    }
}
