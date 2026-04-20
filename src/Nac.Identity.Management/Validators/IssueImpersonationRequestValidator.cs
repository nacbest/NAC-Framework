using FluentValidation;
using Nac.Identity.Management.Contracts.Impersonation;

namespace Nac.Identity.Management.Validators;

/// <summary>
/// Validates <see cref="IssueImpersonationRequest"/> at the API boundary.
/// Mirrors the regex used in <c>ImpersonationService.ValidateReason</c> so the controller
/// returns a structured 400 before the domain layer even runs.
/// </summary>
internal sealed class IssueImpersonationRequestValidator : AbstractValidator<IssueImpersonationRequest>
{
    // Matches service-layer regex: printable word chars, whitespace, and common punctuation.
    private const string ReasonPattern = @"^[\w\s\-#:.,()/]+$";

    public IssueImpersonationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .Matches(ReasonPattern).WithMessage(
                "Reason contains disallowed characters. Allowed: letters, digits, spaces, and -#:.,()/");
    }
}
