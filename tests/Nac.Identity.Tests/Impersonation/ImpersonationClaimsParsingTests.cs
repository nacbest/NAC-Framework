using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nac.Identity.Services;
using NSubstitute;
using Xunit;

namespace Nac.Identity.Tests.Impersonation;

/// <summary>
/// Unit tests for CurrentUserAccessor parsing of `act` claim (ImpersonatorId extraction).
/// Coverage: U3–U4 matrix.
/// </summary>
public class ImpersonationClaimsParsingTests
{
    /// <summary>
    /// U3: CurrentUserAccessor correctly parses `act.sub` into ImpersonatorId.
    /// </summary>
    [Fact]
    public void ImpersonatorId_WithValidActClaim_ReturnsParsedGuid()
    {
        var impersonatorId = Guid.NewGuid();
        var actJson = JsonSerializer.Serialize(new { sub = impersonatorId.ToString() });

        var accessor = CreateAccessor([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(NacIdentityClaims.ActClaim, actJson)
        ]);

        accessor.ImpersonatorId.Should().Be(impersonatorId);
    }

    /// <summary>
    /// U4: CurrentUserAccessor returns null ImpersonatorId without `act` claim.
    /// </summary>
    [Fact]
    public void ImpersonatorId_WithoutActClaim_ReturnsNull()
    {
        var accessor = CreateAccessor([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "user@example.com")
        ]);

        accessor.ImpersonatorId.Should().BeNull();
    }

    /// <summary>
    /// U4 extension: Malformed act claim returns null.
    /// </summary>
    [Fact]
    public void ImpersonatorId_WithMalformedActJson_ReturnsNull()
    {
        var accessor = CreateAccessor([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(NacIdentityClaims.ActClaim, "not-json")
        ]);

        accessor.ImpersonatorId.Should().BeNull();
    }

    /// <summary>
    /// U4 extension: Act claim without `sub` property returns null.
    /// </summary>
    [Fact]
    public void ImpersonatorId_WithActClaimNoSubProperty_ReturnsNull()
    {
        var actJson = JsonSerializer.Serialize(new { other = "value" });
        var accessor = CreateAccessor([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(NacIdentityClaims.ActClaim, actJson)
        ]);

        accessor.ImpersonatorId.Should().BeNull();
    }

    /// <summary>
    /// U3 extension: Non-Guid `sub` value returns null.
    /// </summary>
    [Fact]
    public void ImpersonatorId_WithActClaimNonGuidSub_ReturnsNull()
    {
        var actJson = JsonSerializer.Serialize(new { sub = "not-a-guid" });
        var accessor = CreateAccessor([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(NacIdentityClaims.ActClaim, actJson)
        ]);

        accessor.ImpersonatorId.Should().BeNull();
    }

    private static CurrentUserAccessor CreateAccessor(Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return new CurrentUserAccessor(httpContextAccessor);
    }
}
