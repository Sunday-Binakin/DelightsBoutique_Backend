using DelightsBoutique.Application.Products.Queries;
using FluentValidation;

namespace DelightsBoutique.Application.Products.Validators;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    private static readonly string[] AllowedSorts = ["newest", "price-asc", "price-desc", "rating"];

    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort).Must(s => s is null || AllowedSorts.Contains(s))
            .WithMessage($"Sort must be one of: {string.Join(", ", AllowedSorts)}.");
    }
}
