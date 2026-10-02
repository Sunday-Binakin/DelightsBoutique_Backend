using DelightsBoutique.Application.Categories.Commands;
using FluentValidation;

namespace DelightsBoutique.Application.Categories.Validators;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Tone).IsInEnum();
    }
}
