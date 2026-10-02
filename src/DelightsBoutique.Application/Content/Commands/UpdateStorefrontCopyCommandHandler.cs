using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

/// <summary>
/// "Partial update" on an opaque blob means a shallow top-level JSON key merge — overwrite
/// or insert each key present in the patch, leave every other existing key untouched.
/// No deep merge.
/// </summary>
public class UpdateStorefrontCopyCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<UpdateStorefrontCopyCommand, JsonElement>
{
    public async Task<JsonElement> Handle(UpdateStorefrontCopyCommand request, CancellationToken cancellationToken)
    {
        var currentJson = await siteContent.GetValueJsonAsync(SiteContentKey.StorefrontCopy, cancellationToken);

        var merged = new Dictionary<string, JsonElement>();
        using (var currentDoc = JsonDocument.Parse(currentJson!))
        {
            foreach (var prop in currentDoc.RootElement.EnumerateObject())
                merged[prop.Name] = prop.Value.Clone(); // Clone — currentDoc is disposed at the end of this using block
        }

        if (request.Patch.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in request.Patch.EnumerateObject())
                merged[prop.Name] = prop.Value.Clone(); // top-level overwrite/insert only
        }

        var mergedJson = JsonSerializer.Serialize(merged);
        await siteContent.UpsertAsync(SiteContentKey.StorefrontCopy, mergedJson, cancellationToken);

        using var resultDoc = JsonDocument.Parse(mergedJson);
        return resultDoc.RootElement.Clone();
    }
}
