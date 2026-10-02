using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Products.Queries;

public record GetProductsQuery(string? Category, string? Search, Badge? Badge, string? Sort, int Page = 1, int Limit = 20)
    : IRequest<PagedResult<ProductDto>>;
