using DelightsBoutique.Application.Products.Dtos;

namespace DelightsBoutique.Application.Content.Dtos;

public record CollectionSectionContentDto(
    string Eyebrow, string Title, string NewArrivalsLabel, string BestSellersLabel, string WholesalePicksLabel,
    List<string> NewArrivalsSlugs, List<string> BestSellersSlugs, List<string> WholesalePicksSlugs);

public record CollectionSectionDto(
    string Eyebrow, string Title, string NewArrivalsLabel, string BestSellersLabel, string WholesalePicksLabel,
    List<ProductDto> NewArrivals, List<ProductDto> BestSellers, List<ProductDto> WholesalePicks);
