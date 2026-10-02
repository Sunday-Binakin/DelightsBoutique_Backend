using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

/// <summary>
/// StorefrontCopy is treated as genuinely opaque JSON — BUILD_SPEC defers its shape to a
/// frontend file (storefront-copy.ts) this backend doesn't have access to.
/// </summary>
public class GetStorefrontCopyQueryHandler(ISiteContentRepository siteContent) : IRequestHandler<GetStorefrontCopyQuery, JsonElement>
{
    public async Task<JsonElement> Handle(GetStorefrontCopyQuery request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.StorefrontCopy, cancellationToken);
        using var doc = JsonDocument.Parse(json!);
        return doc.RootElement.Clone(); // MUST Clone — doc is disposed at method end
    }
}
