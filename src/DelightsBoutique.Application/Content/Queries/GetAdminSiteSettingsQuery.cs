using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

// Separate from GetSiteSettingsQuery (public, returns PublicSiteSettingsDto) because there
// was previously no way for an admin to read NotifyByEmail/NotificationEmail/NotifyByWhatsapp
// without first writing a PATCH — this is the missing read path, gated by ManageContent at
// the controller like every other admin content action.
public record GetAdminSiteSettingsQuery : IRequest<SiteSettingsDto>;

public class GetAdminSiteSettingsQueryHandler(ISiteContentRepository siteContent) : IRequestHandler<GetAdminSiteSettingsQuery, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(GetAdminSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.SiteSettings, cancellationToken);
        return JsonSerializer.Deserialize<SiteSettingsDto>(json!, ContentJsonOptions.Default)!;
    }
}
