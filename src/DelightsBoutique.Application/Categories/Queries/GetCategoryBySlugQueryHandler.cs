using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Categories.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Categories.Queries;

public class GetCategoryBySlugQueryHandler(ICategoryRepository categories) : IRequestHandler<GetCategoryBySlugQuery, CategoryDto?>
{
    public Task<CategoryDto?> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken) =>
        categories.GetBySlugAsync(request.Slug, cancellationToken);
}
