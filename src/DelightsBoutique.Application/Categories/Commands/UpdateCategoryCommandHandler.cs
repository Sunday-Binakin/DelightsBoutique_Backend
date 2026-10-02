using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Categories.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Categories.Commands;

public class UpdateCategoryCommandHandler(ICategoryRepository categories) : IRequestHandler<UpdateCategoryCommand, CategoryDto?>
{
    public async Task<CategoryDto?> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetEntityBySlugAsync(request.Slug, cancellationToken);
        if (category is null)
            return null;

        category.Update(request.Name, request.Tone, request.ImageUrl);
        await categories.UpdateAsync(category, cancellationToken);

        return await categories.GetBySlugAsync(category.Slug, cancellationToken);
    }
}
