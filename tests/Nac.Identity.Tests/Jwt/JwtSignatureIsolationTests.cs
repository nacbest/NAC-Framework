using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

/// <summary>
/// R2 guard: forging a JWT by swapping the tenant_id claim or signing it with an
/// unknown key MUST NOT validate. Protects against cross-tenant impersonation.
/// </summary>
public class JwtSignatureIsolationTests
{
    private const string LegitKey = "tenant-a-legit-secret-key-at-least-32-characters-ok";
    private const string AttackerKey = "attacker-fabricated-secret-key-at-least-32-chars-ok";
    private const string Issuer = "IsoIssuer";
    private const string Audience = "IsoAud";

    private static JwtTokenService Service(string key) => new(Options.Create(new JwtOptions
    {
        SecretKey = key, Issuer = Issuer, Audience = Audience, ExpirationMinutes = 60,
    }));

    private static TokenValidationParameters ValidationParams(string key) => new()
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Issuer, ValidAudience = Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
    };

    [Fact]
    public void Token_SignedWithAttackerKey_FailsValidationAgainstLegitKey()
    {
        var forged = Service(AttackerKey).GenerateToken(
            Guid.NewGuid(), "tenant-b", "attacker@example.com", null, [], isHost: false);

        var handler = new JwtSecurityTokenHandler();
        var act = () => handler.ValidateToken(forged, ValidationParams(LegitKey), out _);

        act.Should().Throw<SecurityTokenException>(
            "JWT signed with an unknown key must be rejected by the legit validator");
    }

    [Fact]
    public void Token_WithSwappedSignature_FailsSignatureValidation()
    {
        var tokenA = Service(LegitKey).GenerateToken(
            Guid.NewGuid(), "tenant-a", "a@example.com", null, [], isHost: false);
        var tokenB = Service(LegitKey).GenerateToken(
            Guid.NewGuid(), "tenant-b", "b@example.com", null, [], isHost: false);

        // Take A's header+payload and attach B's signature — signature must not validate.
        var a = tokenA.Split('.');
        var b = tokenB.Split('.');
        var forged = $"{a[0]}.{a[1]}.{b[2]}";

        var handler = new JwtSecurityTokenHandler();
        var act = () => handler.ValidateToken(forged, ValidationParams(LegitKey), out _);

        act.Should().Throw<SecurityTokenException>(
            "JWT where the signature doesn't match the header+payload must be rejected");
    }

    [Fact]
    public void Token_SignedWithLegitKey_ValidatesSuccessfully()
    {
        var token = Service(LegitKey).GenerateToken(
            Guid.NewGuid(), "tenant-a", "u@example.com", null, [], isHost: false);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, ValidationParams(LegitKey), out _);

        principal.Claims.Should().Contain(c =>
            c.Type == NacIdentityClaims.TenantId && c.Value == "tenant-a");
    }
}
