using DelightsBoutique.Application.Auth.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Auth.Validators;

public class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.LoginToken).NotEmpty();
    }
}
