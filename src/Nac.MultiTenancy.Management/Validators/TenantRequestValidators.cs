using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.Options;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Dtos;

namespace Nac.MultiTenancy.Management.Validators;

/// <summary>Validates <see cref="CreateTenantRequest"/>.</summary>
public sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    /// <summary>Lowercase kebab-case slug, 3-50 chars.</summary>
    public static readonly Regex IdentifierRegex =
        new(@"^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$", RegexOptions.Compiled);

    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .Matches(IdentifierRegex)
            .WithMessage("Identifier must be lowercase, 3-50 chars, may contain digits and hyphens.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IsolationMode).IsInEnum();
        RuleFor(x => x.ConnectionString)
            .MaximumLength(2000)
            .NotEmpty().When(x => x.IsolationMode == TenantIsolationMode.Database)
            .WithMessage("ConnectionString is required when IsolationMode is Database.");
    }
}

/// <summary>Validates <see cref="UpdateTenantRequest"/>.</summary>
public sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .NotEmpty().When(x => x.Name is not null)
            .WithMessage("Name cannot be blank when supplied.");
        RuleFor(x => x.ConnectionString)
            .MaximumLength(2000)
            .NotEmpty().When(x => x.IsolationMode == TenantIsolationMode.Database)
            .WithMessage("ConnectionString is required when switching to Database isolation.");
        RuleFor(x => x.IsolationMode!.Value)
            .IsInEnum().When(x => x.IsolationMode is not null);
    }
}

/// <summary>Validates <see cref="BulkTenantRequest"/>.</summary>
public sealed class BulkTenantRequestValidator : AbstractValidator<BulkTenantRequest>
{
    public BulkTenantRequestValidator(IOptions<TenantManagementOptions> options)
    {
        var max = options.Value.MaxBulkSize;
        RuleFor(x => x.Ids)
            .NotEmpty()
            .Must(ids => ids.Count <= max)
                .WithMessage($"At most {max} ids per bulk call.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Ids must be distinct.");
    }
}
