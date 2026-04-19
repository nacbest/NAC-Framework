using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

public class JwtTokenServiceTests
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
    public async Task GenerateTokenAsync_WithValidUser_ProducesValidJwt()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateTokenAsync_ProducedToken_ContainsNameIdentifierClaim()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var claims = DecodeToken(token);

        // Assert
        claims.Should().ContainSingle(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
    }

    [Fact]
    public async Task GenerateTokenAsync_ProducedToken_ContainsEmailClaim()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var claims = DecodeToken(token);

        // Assert
        claims.Should().ContainSingle(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    [Fact]
    public async Task GenerateTokenAsync_ProducedToken_ContainsTenantIdClaim()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var claims = DecodeToken(token);

        // Assert
        claims.Should().ContainSingle(c =>
            c.Type == NacIdentityClaims.TenantId && c.Value == user.TenantId);
    }


    [Fact]
    public async Task GenerateTokenAsync_ProducedToken_ContainsCorrectIssuer()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        jwtToken!.Issuer.Should().Be(TestIssuer);
    }

    [Fact]
    public async Task GenerateTokenAsync_ProducedToken_ContainsCorrectAudience()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);

        // Act
        var token = await service.GenerateTokenAsync(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        jwtToken!.Audiences.Should().Contain(TestAudience);
    }

    [Fact]
    public async Task GenerateTokenAsync_ProducedToken_ExpiresAtCorrectTime()
    {
        // Arrange
        var user = CreateTestUser();
        var userManager = CreateMockUserManager(user, roles: [], claims: []);
        var options = Options.Create(_jwtOptions);
        var service = new JwtTokenService(options, userManager);
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = await service.GenerateTokenAsync(user);
        var afterGeneration = DateTime.UtcNow;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        // Assert
        var expectedExpiration = beforeGeneration.AddMinutes(TestExpirationMinutes);
        jwtToken!.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
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
