using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

public class UpdateProductCommandHandler(IProductRepository products) : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetEntityBySlugAsync(request.Slug, cancellationToken);
        if (product is null)
            return null;

        product.Update(
            request.Name, request.CategorySlug, request.Description,
            request.RetailPrice, request.WholesalePrice, request.WholesaleMoq, request.CompareAtPrice,
            request.Rating, request.ReviewCount, request.Tone, request.Badge, request.StockStatus,
            request.Sizes, request.Colors, request.ImageUrls);

        await products.UpdateAsync(product, cancellationToken);

        return await products.GetBySlugAsync(product.Slug, cancellationToken);
    }
}
