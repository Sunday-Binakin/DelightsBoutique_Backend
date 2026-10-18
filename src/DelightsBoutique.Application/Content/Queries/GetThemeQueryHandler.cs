using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public class GetThemeQueryHandler(ISiteContentRepository siteContent) : IRequestHandler<GetThemeQuery, ThemeDto>
{
    public async Task<ThemeDto> Handle(GetThemeQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.Theme, cancellationToken);
        return JsonSerializer.Deserialize<ThemeDto>(json!, ContentJsonOptions.Default)!;
    }
}
