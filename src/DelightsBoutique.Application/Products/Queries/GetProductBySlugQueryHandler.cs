using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Products.Queries;

public class GetProductBySlugQueryHandler(IProductRepository products) : IRequestHandler<GetProductBySlugQuery, ProductDto?>
{
    public Task<ProductDto?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken) =>
        products.GetBySlugAsync(request.Slug, cancellationToken);
}
