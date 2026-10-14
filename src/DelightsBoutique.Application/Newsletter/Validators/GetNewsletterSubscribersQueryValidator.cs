using DelightsBoutique.Application.Newsletter.Queries;
using FluentValidation;

namespace DelightsBoutique.Application.Newsletter.Validators;

public class GetNewsletterSubscribersQueryValidator : AbstractValidator<GetNewsletterSubscribersQuery>
{
    public GetNewsletterSubscribersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
