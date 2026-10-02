using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

public record CreateProductCommand(
    string? Slug, string Name, string CategorySlug, string Description,
    decimal RetailPrice, decimal? WholesalePrice, int? WholesaleMoq, decimal? CompareAtPrice,
    decimal? Rating, int? ReviewCount, Tone Tone, Badge? Badge, StockStatus StockStatus,
    List<string>? Sizes, List<string>? Colors, List<string>? ImageUrls)
    : IRequest<ProductDto>, IAuditableCommand
{
    public string DescribeAction() => $"Created product '{Slug ?? Name}'";
}
