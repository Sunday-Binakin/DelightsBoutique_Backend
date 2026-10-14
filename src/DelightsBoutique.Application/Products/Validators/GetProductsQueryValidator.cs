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
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice is not null);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice is not null);
        // Deliberately no "MaxPrice >= MinPrice" rule — an inverted range just yields an
        // empty page naturally (same WHERE clause either way), matching the "unknown/edge
        // filter values return an empty page, not an error" rule the frontend asked for.
    }
}
