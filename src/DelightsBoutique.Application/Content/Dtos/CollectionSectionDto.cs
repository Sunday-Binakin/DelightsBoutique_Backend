using DelightsBoutique.Application.Products.Dtos;

namespace DelightsBoutique.Application.Content.Dtos;

public record CollectionSectionContentDto(
    string Eyebrow, string Title, string NewArrivalsLabel, string BestSellersLabel, string WholesalePicksLabel,
    List<string> NewArrivalsSlugs, List<string> BestSellersSlugs, List<string> WholesalePicksSlugs);

/// <summary>Each *Curated flag is false when its slug list was empty and the matching
/// list was auto-filled instead (newest/BestSeller-badged/wholesale-eligible products).</summary>
public record CollectionSectionDto(
    string Eyebrow, string Title, string NewArrivalsLabel, string BestSellersLabel, string WholesalePicksLabel,
    List<ProductDto> NewArrivals, bool NewArrivalsCurated,
    List<ProductDto> BestSellers, bool BestSellersCurated,
    List<ProductDto> WholesalePicks, bool WholesalePicksCurated);
