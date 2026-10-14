using DelightsBoutique.Application.Account.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Account.Validators;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        // The first endpoint in this API where a human chooses their own password —
        // every other account's password up to now has been system-generated (see
        // IPasswordGenerator), so this is the first place that needed a strength rule at all.
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
