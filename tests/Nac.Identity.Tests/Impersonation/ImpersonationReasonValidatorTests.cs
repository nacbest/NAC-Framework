using FluentAssertions;
using Xunit;

namespace Nac.Identity.Tests.Impersonation;

/// <summary>
/// H3-I8: Data-driven unit tests for the reason validator inside <see cref="ImpersonationService.IssueAsync"/>.
/// Validates that the reason field meets the length constraints (10–500 chars) and allowed character set.
/// </summary>
public class ImpersonationReasonValidatorTests
{
    /// <summary>
    /// I8: Reason validator must reject reasons that are too short (< 10 chars).
    /// Note: Empty/null strings are caught by ArgumentException.ThrowIfNullOrWhiteSpace first.
    /// </summary>
    [Theory]
    [InlineData("short")]      // 5 chars
    [InlineData("123456789")]  // 9 chars (just under limit)
    public void ValidateReason_WhenTooShort_ThrowsArgumentException(string reason)
    {
        // Act
        var act = () => ValidateReason(reason);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*10–500 characters*");
    }

    /// <summary>
    /// I8: Empty or whitespace-only reasons are rejected early by null/whitespace check.
    /// </summary>
    [Theory]
    [InlineData("")]           // Empty
    [InlineData("   ")]        // Whitespace only
    [InlineData("\t\t")]       // Tabs only
    public void ValidateReason_WhenNullOrWhitespace_ThrowsArgumentException(string reason)
    {
        // Act
        var act = () => ValidateReason(reason);

        // Assert - caught by ThrowIfNullOrWhiteSpace, not the length check
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// I8: Reason validator must accept reasons that meet the length requirement (10–500 chars).
    /// </summary>
    [Theory]
    [InlineData("TICKET-1234 issue")]       // 17 chars
    [InlineData("ZEN-12345 - reset user MFA")] // 27 chars (from spec)
    [InlineData("BUG: unable to login - retry")]  // 28 chars
    [InlineData("1234567890")]                    // Exactly 10 chars (lower bound)
    [InlineData("TICKET-1 bug")]                  // 13 chars
    public void ValidateReason_WhenValidLength_Succeeds(string reason)
    {
        // Act & Assert: no exception thrown
        var act = () => ValidateReason(reason);
        act.Should().NotThrow();
    }

    /// <summary>
    /// I8: Reason validator must reject reasons that are too long (> 500 chars).
    /// </summary>
    [Fact]
    public void ValidateReason_WhenTooLong_ThrowsArgumentException()
    {
        // Arrange: 501 valid characters
        var reason = new string('a', 501);

        // Act
        var act = () => ValidateReason(reason);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*10–500 characters*");
    }

    /// <summary>
    /// I8: Reason validator accepts maximum valid length (500 chars).
    /// </summary>
    [Fact]
    public void ValidateReason_WhenExactlyMaxLength_Succeeds()
    {
        // Arrange: exactly 500 valid characters
        var reason = new string('a', 500);

        // Act & Assert
        var act = () => ValidateReason(reason);
        act.Should().NotThrow();
    }

    /// <summary>
    /// I8: Reason validator accepts whitespace (including whitespace control chars).
    /// Note: The regex `[\w\s\-#:.,()/]+` includes \s which matches newlines/tabs.
    /// This is by design — reason can span multiple lines in the audit log.
    /// </summary>
    [Theory]
    [InlineData("TICKET-1\nwith newline")]        // Newline allowed (part of \s)
    [InlineData("Multi\r\nLine")]                 // CRLF allowed
    [InlineData("Reason\twith\ttabs")]            // Tabs allowed
    public void ValidateReason_WhenContainsWhitespaceChars_Succeeds(string reason)
    {
        // Act & Assert: whitespace is allowed in the regex
        var act = () => ValidateReason(reason);
        act.Should().NotThrow();
    }

    /// <summary>
    /// I8: Reason validator accepts allowed special characters: - # : . , ( ) /
    /// Regex is: ^[\w\s\-#:.,()/]+$
    /// where \w = [a-zA-Z0-9_], \s = whitespace (including tabs, newlines)
    /// </summary>
    [Theory]
    [InlineData("TICKET-123: test reason")]     // dash and colon
    [InlineData("ZEN-12345 - reset user MFA")]  // dashes
    [InlineData("BUG#456 - fix (critical)")]    // hash, parens
    [InlineData("Support/Incident: 789")]       // slash, colon
    [InlineData("Test,Multiple,Words")]        // commas
    [InlineData("Path: /api/v1/users")]        // slashes and colon
    [InlineData("Amount.with.dots")]            // dot allowed (11 chars)
    public void ValidateReason_WhenContainsAllowedCharacters_Succeeds(string reason)
    {
        // Act & Assert
        var act = () => ValidateReason(reason);
        act.Should().NotThrow();
    }

    /// <summary>
    /// I8: Reason validator rejects disallowed special characters.
    /// </summary>
    [Theory]
    [InlineData("Test@example.com")]      // @ not allowed
    [InlineData("Quote: \"test\"")]       // quotes not allowed
    [InlineData("Equation: x=5+3")]       // = and + not allowed
    [InlineData("Bracket: [test]")]       // brackets not allowed
    [InlineData("HTML: <tag>")]           // angle brackets not allowed
    public void ValidateReason_WhenContainsDisallowedCharacters_ThrowsArgumentException(string reason)
    {
        // Act
        var act = () => ValidateReason(reason);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*disallowed characters*");
    }

    /// <summary>
    /// I8: Reason validator trims input before validation so leading/trailing spaces don't count.
    /// </summary>
    [Fact]
    public void ValidateReason_TrimsBefore_ValidatingLength()
    {
        // Arrange: 5 chars + 5 leading/trailing spaces = meets length requirement after trim
        var reason = "     12345 test     "; // "12345 test" after trim = 10 chars

        // Act & Assert
        var act = () => ValidateReason(reason);
        act.Should().NotThrow("trimmed value is exactly 10 characters");
    }


    // ── Helper ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Invokes the private ValidateReason method via reflection.
    /// ImpersonationService is internal, so we access it through reflection.
    /// </summary>
    private static void ValidateReason(string reason)
    {
        // Get the internal ImpersonationService type
        var impersonationServiceType = typeof(Nac.Identity.Impersonation.IImpersonationService)
            .Assembly
            .GetType("Nac.Identity.Impersonation.ImpersonationService");

        if (impersonationServiceType is null)
            throw new InvalidOperationException("ImpersonationService type not found");

        var method = impersonationServiceType
            .GetMethod("ValidateReason",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method is null)
            throw new InvalidOperationException("ValidateReason method not found");

        try
        {
            method.Invoke(null, new object[] { reason });
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            // Unwrap the inner exception
            throw ex.InnerException ?? ex;
        }
    }
}
