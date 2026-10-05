using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Products.Queries;

public record GetProductFacetsQuery(string? Category, string? Search, Badge? Badge, decimal? MinPrice = null, decimal? MaxPrice = null)
    : IRequest<ProductFacetsDto>;

public class GetProductFacetsQueryHandler(IProductRepository products) : IRequestHandler<GetProductFacetsQuery, ProductFacetsDto>
{
    public Task<ProductFacetsDto> Handle(GetProductFacetsQuery request, CancellationToken ct) =>
        products.GetFacetsAsync(new ProductFacetsFilter(request.Category, request.Search, request.Badge, request.MinPrice, request.MaxPrice), ct);
}
