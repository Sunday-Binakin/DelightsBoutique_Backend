using DelightsBoutique.Application.Auth.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Auth.Validators;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.LoginToken).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").WithMessage("Code must be exactly 6 digits.");
    }
}
