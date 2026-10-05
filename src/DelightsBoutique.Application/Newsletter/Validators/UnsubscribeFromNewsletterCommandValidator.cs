using DelightsBoutique.Application.Newsletter.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Newsletter.Validators;

public class UnsubscribeFromNewsletterCommandValidator : AbstractValidator<UnsubscribeFromNewsletterCommand>
{
    public UnsubscribeFromNewsletterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
