using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Products.Queries;

public class GetProductsQueryHandler(IProductRepository products) : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken) =>
        products.SearchAsync(new ProductSearchFilter(request.Category, request.Search, request.Badge, request.Sort, request.Page, request.Limit), cancellationToken);
}
