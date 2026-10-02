using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Domain.Enums;
using FluentValidation;

namespace DelightsBoutique.Application.Orders.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty(); // defense-in-depth; the real HTTP-edge guard is controller-level
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Slug).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Address).NotEmpty().When(x => x.Fulfillment == FulfillmentType.Delivery);
    }
}
