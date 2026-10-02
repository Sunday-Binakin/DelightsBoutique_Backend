using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Categories.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Categories.Queries;

public class GetCategoriesQueryHandler(ICategoryRepository categories) : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    public Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken) =>
        categories.GetAllAsync(cancellationToken);
}
