using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public class GetSiteSettingsQueryHandler(ISiteContentRepository siteContent) : IRequestHandler<GetSiteSettingsQuery, PublicSiteSettingsDto>
{
    public async Task<PublicSiteSettingsDto> Handle(GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.SiteSettings, cancellationToken);
        var full = JsonSerializer.Deserialize<SiteSettingsDto>(json!, ContentJsonOptions.Default)!;
        return PublicSiteSettingsDto.FromFull(full);
    }
}
