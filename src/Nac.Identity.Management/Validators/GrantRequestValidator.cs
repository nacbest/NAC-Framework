using FluentValidation;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Permissions;

namespace Nac.Identity.Management.Validators;

/// <summary>
/// Validates <see cref="GrantRequest"/> — ensures the permission name is registered
/// in the <see cref="PermissionDefinitionManager"/> to catch typos at the boundary.
/// </summary>
internal sealed class GrantRequestValidator : AbstractValidator<GrantRequest>
{
    public GrantRequestValidator(PermissionDefinitionManager definitionManager)
    {
        RuleFor(x => x.PermissionName)
            .NotEmpty()
            .Must(name => definitionManager.GetOrNull(name) is not null)
            .WithMessage(x => $"Permission '{x.PermissionName}' is not registered.");
    }
}
