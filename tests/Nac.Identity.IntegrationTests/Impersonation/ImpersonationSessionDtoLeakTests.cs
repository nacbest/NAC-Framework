using FluentAssertions;
using Nac.Identity.Impersonation;
using Nac.Identity.Management.Contracts.Impersonation;
using System.Text.Json;
using Xunit;

namespace Nac.Identity.IntegrationTests.Impersonation;

/// <summary>
/// H3-I10: Verifies that <see cref="ImpersonationSessionDto"/> does NOT expose the
/// <c>jti</c> field when serialized to JSON. The <c>jti</c> is an internal revocation
/// key; leaking it would allow bearer-replay attacks against the blacklist.
/// This is a pure unit test (no WebApp or DB required).
/// </summary>
public class ImpersonationSessionDtoLeakTests
{
    /// <summary>
    /// I10: Serializing <see cref="ImpersonationSessionDto"/> to JSON must NOT produce
    /// a "jti" key (case-insensitive). Validated via both JSON string inspection and
    /// type-level reflection so the assertion survives future property renames.
    /// </summary>
    [Fact]
    public void ImpersonationSessionDto_JsonSerialization_DoesNotContainJtiKey()
    {
        // Arrange: build a session aggregate and project it through the DTO factory
        var session = ImpersonationSession.Issue(
            hostUserId: Guid.NewGuid(),
            tenantId: "tenant-leak-check",
            reason: "I10 DTO leak regression test",
            jti: Guid.NewGuid().ToString("N"),
            ttl: TimeSpan.FromMinutes(15));

        var dto = ImpersonationSessionDto.From(session);

        // Act: serialize with camelCase policy (matches production ASP.NET Core defaults)
        var json = JsonSerializer.Serialize(dto,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Assert 1: no "jti" key in the JSON output (case-insensitive pattern)
        json.Should().NotMatchRegex(@"(?i)""jti""\s*:",
            because: "Jti is an internal revocation key and must not appear in the API response body");

        // Assert 2: the DTO record type has no property named Jti at compile-time
        // (guards against future accidental addition)
        var jtiProp = typeof(ImpersonationSessionDto)
            .GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "Jti", StringComparison.OrdinalIgnoreCase));
        jtiProp.Should().BeNull(
            "ImpersonationSessionDto must not declare a Jti property — it would expose the revocation key");
    }

    /// <summary>
    /// I10 extension: Verify all 7 expected fields ARE present so the DTO is not just
    /// an empty record that passes the leak check trivially.
    /// Expected fields: id, hostUserId, tenantId, reason, issuedAt, expiresAt, revokedAt.
    /// </summary>
    [Fact]
    public void ImpersonationSessionDto_JsonSerialization_ContainsExpectedFields()
    {
        // Arrange
        var session = ImpersonationSession.Issue(
            hostUserId: Guid.NewGuid(),
            tenantId: "tenant-fields-check",
            reason: "Field completeness check",
            jti: Guid.NewGuid().ToString("N"),
            ttl: TimeSpan.FromMinutes(15));

        var dto = ImpersonationSessionDto.From(session);

        // Act
        var json = JsonSerializer.Serialize(dto,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Assert: expected camelCase keys are present
        json.Should().Contain("\"id\"");
        json.Should().Contain("\"hostUserId\"");
        json.Should().Contain("\"tenantId\"");
        json.Should().Contain("\"reason\"");
        json.Should().Contain("\"issuedAt\"");
        json.Should().Contain("\"expiresAt\"");
        json.Should().Contain("\"revokedAt\"");
    }
}
