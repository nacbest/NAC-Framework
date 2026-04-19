using FluentValidation;
using Nac.Identity.Management.Contracts;

namespace Nac.Identity.Management.Validators;

/// <summary>Validates <see cref="InviteRequest"/> before processing an invitation.</summary>
internal sealed class InviteRequestValidator : AbstractValidator<InviteRequest>
{
    public InviteRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.RoleIds)
            .NotNull()
            .Must(ids => ids.Count > 0).WithMessage("At least one role is required.")
            .Must(ids => ids.Count <= 20).WithMessage("Maximum 20 roles per invitation.");
    }
}
