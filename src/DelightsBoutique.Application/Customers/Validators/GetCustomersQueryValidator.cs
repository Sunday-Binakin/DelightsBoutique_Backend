using DelightsBoutique.Application.Customers.Queries;
using FluentValidation;

namespace DelightsBoutique.Application.Customers.Validators;

public class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
