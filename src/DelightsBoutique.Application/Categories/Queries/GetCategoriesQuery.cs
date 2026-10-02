using DelightsBoutique.Application.Categories.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Categories.Queries;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>;
