using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Categories.Dtos;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Exceptions;
using MediatR;

namespace DelightsBoutique.Application.Categories.Commands;

public class CreateCategoryCommandHandler(ICategoryRepository categories) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugGenerator.FromName(request.Name) : request.Slug!;

        if (await categories.SlugExistsAsync(slug, cancellationToken))
            throw new DuplicateSlugException(slug);

        var category = Category.Create(slug, request.Name, request.Tone, request.ImageUrl);
        await categories.AddAsync(category, cancellationToken);

        return (await categories.GetBySlugAsync(category.Slug, cancellationToken))!;
    }
}
