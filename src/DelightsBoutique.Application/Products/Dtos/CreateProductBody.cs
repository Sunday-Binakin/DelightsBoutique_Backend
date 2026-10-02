using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Products.Dtos;

public record CreateProductBody(
    string? Slug, string Name, string CategorySlug, string Description,
    decimal RetailPrice, decimal? WholesalePrice, int? WholesaleMoq, decimal? CompareAtPrice,
    decimal? Rating, int? ReviewCount, Tone Tone, Badge? Badge, StockStatus StockStatus,
    List<string>? Sizes, List<string>? Colors, List<string>? ImageUrls);
