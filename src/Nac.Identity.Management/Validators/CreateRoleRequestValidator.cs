using FluentValidation;
using Nac.Identity.Management.Contracts;

namespace Nac.Identity.Management.Validators;

/// <summary>Validates <see cref="CreateRoleRequest"/> for custom role creation.</summary>
internal sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(64);

        RuleFor(x => x.Description)
            .MaximumLength(256)
            .When(x => x.Description is not null);
    }
}
