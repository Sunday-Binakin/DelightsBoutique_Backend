using DelightsBoutique.Application.Account.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Account.Validators;

public class UpdateAccountProfileCommandValidator : AbstractValidator<UpdateAccountProfileCommand>
{
    public UpdateAccountProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name is not null);
    }
}
