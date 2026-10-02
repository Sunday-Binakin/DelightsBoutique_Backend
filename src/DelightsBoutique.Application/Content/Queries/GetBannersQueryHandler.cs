using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public class GetBannersQueryHandler(ISiteContentRepository siteContent) : IRequestHandler<GetBannersQuery, BannerSetDto>
{
    public async Task<BannerSetDto> Handle(GetBannersQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.Banners, cancellationToken);
        return JsonSerializer.Deserialize<BannerSetDto>(json!, ContentJsonOptions.Default)!;
    }
}
