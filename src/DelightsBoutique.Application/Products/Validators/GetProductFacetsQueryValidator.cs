using DelightsBoutique.Application.Products.Queries;
using FluentValidation;

namespace DelightsBoutique.Application.Products.Validators;

public class GetProductFacetsQueryValidator : AbstractValidator<GetProductFacetsQuery>
{
    public GetProductFacetsQueryValidator()
    {
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice is not null);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice is not null);
    }
}
