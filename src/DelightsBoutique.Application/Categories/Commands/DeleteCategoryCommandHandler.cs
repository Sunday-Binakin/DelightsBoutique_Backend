using DelightsBoutique.Application.Abstractions;
using MediatR;

namespace DelightsBoutique.Application.Categories.Commands;

public class DeleteCategoryCommandHandler(ICategoryRepository categories) : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetEntityBySlugAsync(request.Slug, cancellationToken);
        if (category is null)
            return false;

        var isReferenced = await categories.IsReferencedByAnyProductAsync(request.Slug, cancellationToken);
        category.EnsureDeletable(isReferenced); // throws CategoryInUseException -> 409

        await categories.DeleteAsync(category, cancellationToken);
        return true;
    }
}
