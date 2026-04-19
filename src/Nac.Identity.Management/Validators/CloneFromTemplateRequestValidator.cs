using FluentValidation;
using Nac.Identity.Management.Contracts;

namespace Nac.Identity.Management.Validators;

/// <summary>Validates <see cref="CloneFromTemplateRequest"/> for template-based role creation.</summary>
internal sealed class CloneFromTemplateRequestValidator : AbstractValidator<CloneFromTemplateRequest>
{
    public CloneFromTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateRoleId)
            .NotEmpty().WithMessage("TemplateRoleId must be a non-empty GUID.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(64);

        RuleFor(x => x.Description)
            .MaximumLength(256)
            .When(x => x.Description is not null);
    }
}
