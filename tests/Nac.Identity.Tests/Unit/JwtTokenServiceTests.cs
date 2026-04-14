using FluentAssertions;
using Nac.Identity.Services;
using Nac.Identity.Tests.Fixtures;
using Xunit;

namespace Nac.Identity.Tests.Unit;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public async Task GenerateTokensAsync_ValidUser_ReturnsTokens()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);
        var service = new JwtTokenService(options, store, userManager);
        var user = TestFixtures.CreateUser();

        // Act
        var result = await service.GenerateTokensAsync(user, "tenant-1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.AccessTokenExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        result.RefreshTokenExpiresAt.Should().BeAfter(result.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task GenerateTokensAsync_StoresRefreshToken()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);
        var service = new JwtTokenService(options, store, userManager);
        var user = TestFixtures.CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        await service.GenerateTokensAsync(user);

        // Assert
        var storedTokens = dbContext.RefreshTokens.ToList();
        storedTokens.Should().HaveCount(1);
        storedTokens[0].UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task RefreshTokensAsync_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);
        var service = new JwtTokenService(options, store, userManager);
        var user = TestFixtures.CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var original = await service.GenerateTokensAsync(user);

        // Act
        var refreshed = await service.RefreshTokensAsync(original.RefreshToken);

        // Assert
        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBe(original.AccessToken);
        refreshed.RefreshToken.Should().NotBe(original.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokensAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);
        var service = new JwtTokenService(options, store, userManager);

        // Act
        var result = await service.RefreshTokensAsync("invalid-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllTokensAsync_RevokesUserTokens()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);
        var service = new JwtTokenService(options, store, userManager);
        var user = TestFixtures.CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        await service.GenerateTokensAsync(user);
        await service.GenerateTokensAsync(user);

        // Act
        await service.RevokeAllTokensAsync(user.Id);

        // Assert
        var activeTokens = dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToList();
        activeTokens.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_MissingSigningKey_ThrowsException()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions(signingKey: "");
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);

        // Act & Assert
        var action = () => new JwtTokenService(options, store, userManager);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*SigningKey*");
    }

    [Fact]
    public void Constructor_ShortSigningKey_ThrowsException()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions(signingKey: "short-key");
        var store = new EfRefreshTokenStore(dbContext);
        var userManager = TestFixtures.CreateUserManager(dbContext);

        // Act & Assert
        var action = () => new JwtTokenService(options, store, userManager);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 characters*");
    }
}
