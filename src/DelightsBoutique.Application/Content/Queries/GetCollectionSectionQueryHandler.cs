using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public class GetCollectionSectionQueryHandler(ISiteContentRepository siteContent, IProductRepository products)
    : IRequestHandler<GetCollectionSectionQuery, CollectionSectionDto>
{
    private const int AutoFillLimit = 8;

    public async Task<CollectionSectionDto> Handle(GetCollectionSectionQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.CollectionSection, cancellationToken);
        var stored = JsonSerializer.Deserialize<CollectionSectionContentDto>(json!, ContentJsonOptions.Default)!;

        var newArrivalsCurated = stored.NewArrivalsSlugs.Count > 0;
        var newArrivals = newArrivalsCurated
            ? await products.GetBySlugsAsync(stored.NewArrivalsSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.Newest, AutoFillLimit, cancellationToken);

        var bestSellersCurated = stored.BestSellersSlugs.Count > 0;
        var bestSellers = bestSellersCurated
            ? await products.GetBySlugsAsync(stored.BestSellersSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.BestSeller, AutoFillLimit, cancellationToken);

        var wholesalePicksCurated = stored.WholesalePicksSlugs.Count > 0;
        var wholesalePicks = wholesalePicksCurated
            ? await products.GetBySlugsAsync(stored.WholesalePicksSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.WholesaleEligible, AutoFillLimit, cancellationToken);

        return new CollectionSectionDto(
            stored.Eyebrow, stored.Title, stored.NewArrivalsLabel, stored.BestSellersLabel, stored.WholesalePicksLabel,
            newArrivals, newArrivalsCurated, bestSellers, bestSellersCurated, wholesalePicks, wholesalePicksCurated);
    }
}
