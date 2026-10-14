using DelightsBoutique.Application.Newsletter.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Newsletter.Validators;

public class SubscribeToNewsletterCommandValidator : AbstractValidator<SubscribeToNewsletterCommand>
{
    public SubscribeToNewsletterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
