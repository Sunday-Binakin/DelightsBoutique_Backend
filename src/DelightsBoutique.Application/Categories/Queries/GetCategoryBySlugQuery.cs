using DelightsBoutique.Application.Categories.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Categories.Queries;

public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDto?>;
