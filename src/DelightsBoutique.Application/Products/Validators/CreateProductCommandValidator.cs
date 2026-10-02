using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(ICategoryRepository categories)
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.RetailPrice).GreaterThan(0);
        RuleFor(x => x.WholesaleMoq).GreaterThan(0).When(x => x.WholesalePrice is not null);
        RuleFor(x => x.Tone).IsInEnum();
        RuleFor(x => x.StockStatus).IsInEnum();
        RuleFor(x => x.CategorySlug)
            .NotEmpty()
            .MustAsync(async (slug, ct) => await categories.SlugExistsAsync(slug, ct))
            .WithMessage(x => $"Category '{x.CategorySlug}' does not exist.");
    }
}
