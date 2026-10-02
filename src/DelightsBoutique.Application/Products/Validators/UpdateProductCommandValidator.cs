using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Products.Validators;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(ICategoryRepository categories)
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name is not null);
        RuleFor(x => x.Description).NotEmpty().When(x => x.Description is not null);
        RuleFor(x => x.RetailPrice).GreaterThan(0).When(x => x.RetailPrice is not null);
        RuleFor(x => x.WholesaleMoq).GreaterThan(0).When(x => x.WholesaleMoq is not null);
        RuleFor(x => x.CategorySlug)
            .MustAsync(async (slug, ct) => slug is null || await categories.SlugExistsAsync(slug, ct))
            .WithMessage(x => $"Category '{x.CategorySlug}' does not exist.");
    }
}
