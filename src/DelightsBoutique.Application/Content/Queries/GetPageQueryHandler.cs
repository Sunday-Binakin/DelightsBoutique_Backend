using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public class GetPageQueryHandler(ISiteContentRepository siteContent) : IRequestHandler<GetPageQuery, PageContentDto?>
{
    public async Task<PageContentDto?> Handle(GetPageQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.PagesContent, cancellationToken);
        var pages = JsonSerializer.Deserialize<PagesContentDto>(json!, ContentJsonOptions.Default)!;
        return PageKeys.Select(pages, request.Key);
    }
}
