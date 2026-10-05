using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public class GetFlashSaleQueryHandler(ISiteContentRepository siteContent, IProductRepository products)
    : IRequestHandler<GetFlashSaleQuery, FlashSaleDto>
{
    // Arbitrary but reasonable cap on an auto-filled list — the admin's own curated list
    // has no such cap, since that's an explicit choice they made.
    private const int AutoFillLimit = 8;

    public async Task<FlashSaleDto> Handle(GetFlashSaleQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.FlashSale, cancellationToken);
        var stored = JsonSerializer.Deserialize<FlashSaleContentDto>(json!, ContentJsonOptions.Default)!;

        var isCurated = stored.ProductSlugs.Count > 0;
        var resolved = isCurated
            ? await products.GetBySlugsAsync(stored.ProductSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.Discounted, AutoFillLimit, cancellationToken);

        return new FlashSaleDto(stored.Eyebrow, stored.Title, stored.EndsAt, resolved, isCurated);
    }
}
