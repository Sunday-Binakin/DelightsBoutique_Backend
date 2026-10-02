using DelightsBoutique.Application.Orders.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Orders.Validators;

public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Status is not null || x.InternalNotes is not null)
            .WithMessage("At least one of status or internalNotes must be supplied.")
            .OverridePropertyName("status");
    }
}
