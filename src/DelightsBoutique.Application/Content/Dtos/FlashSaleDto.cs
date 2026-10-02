using DelightsBoutique.Application.Products.Dtos;

namespace DelightsBoutique.Application.Content.Dtos;

/// <summary>Stored shape — holds only slugs.</summary>
public record FlashSaleContentDto(string Eyebrow, string Title, DateTime EndsAt, List<string> ProductSlugs);

/// <summary>Resolved shape — slugs replaced with full products, so the storefront needs no second round trip.</summary>
public record FlashSaleDto(string Eyebrow, string Title, DateTime EndsAt, List<ProductDto> Products);
