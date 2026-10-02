using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Exceptions;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

public class CreateProductCommandHandler(IProductRepository products) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? SlugGenerator.FromName(request.Name) : request.Slug!;

        if (await products.SlugExistsAsync(slug, cancellationToken))
            throw new DuplicateSlugException(slug);

        var product = Product.Create(
            slug, request.Name, request.CategorySlug, request.Description,
            request.RetailPrice, request.WholesalePrice, request.WholesaleMoq, request.CompareAtPrice,
            request.Tone, request.Badge, request.StockStatus,
            request.Sizes, request.Colors, request.ImageUrls,
            request.Rating ?? 0, request.ReviewCount ?? 0);

        await products.AddAsync(product, cancellationToken);

        return (await products.GetBySlugAsync(product.Slug, cancellationToken))!;
    }
}
