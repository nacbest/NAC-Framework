using FluentValidation;

namespace Orders.Features.CreateOrder;

/// <summary>FluentValidation rules for <see cref="CreateOrderCommand"/>.</summary>
internal sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(c => c.Items)
            .NotEmpty()
            .WithMessage("An order must contain at least one item.");

        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("Item quantity must be greater than 0.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Item unit price must be >= 0.");

            item.RuleFor(i => i.ProductId)
                .NotEmpty()
                .WithMessage("ProductId must not be empty.");
        });
    }
}
