using DelightsBoutique.Application.Products.Dtos;

namespace DelightsBoutique.Application.Content.Dtos;

/// <summary>Stored shape — holds only slugs.</summary>
public record FlashSaleContentDto(string Eyebrow, string Title, DateTime EndsAt, List<string> ProductSlugs);

/// <summary>Resolved shape — slugs replaced with full products, so the storefront needs no
/// second round trip. IsCurated is false when ProductSlugs was empty and Products was
/// auto-filled from discounted products instead of the admin's own picks.</summary>
public record FlashSaleDto(string Eyebrow, string Title, DateTime EndsAt, List<ProductDto> Products, bool IsCurated);
