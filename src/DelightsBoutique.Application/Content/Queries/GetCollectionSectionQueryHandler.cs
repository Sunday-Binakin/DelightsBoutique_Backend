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
    public async Task<CollectionSectionDto> Handle(GetCollectionSectionQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.CollectionSection, cancellationToken);
        var stored = JsonSerializer.Deserialize<CollectionSectionContentDto>(json!, ContentJsonOptions.Default)!;

        var newArrivals = await products.GetBySlugsAsync(stored.NewArrivalsSlugs, cancellationToken);
        var bestSellers = await products.GetBySlugsAsync(stored.BestSellersSlugs, cancellationToken);
        var wholesalePicks = await products.GetBySlugsAsync(stored.WholesalePicksSlugs, cancellationToken);

        return new CollectionSectionDto(
            stored.Eyebrow, stored.Title, stored.NewArrivalsLabel, stored.BestSellersLabel, stored.WholesalePicksLabel,
            newArrivals, bestSellers, wholesalePicks);
    }
}
