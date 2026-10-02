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
    public async Task<FlashSaleDto> Handle(GetFlashSaleQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.FlashSale, cancellationToken);
        var stored = JsonSerializer.Deserialize<FlashSaleContentDto>(json!, ContentJsonOptions.Default)!;

        var resolved = await products.GetBySlugsAsync(stored.ProductSlugs, cancellationToken);

        return new FlashSaleDto(stored.Eyebrow, stored.Title, stored.EndsAt, resolved);
    }
}
