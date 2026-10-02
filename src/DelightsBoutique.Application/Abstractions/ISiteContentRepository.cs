using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Abstractions;

public interface ISiteContentRepository
{
    Task<string?> GetValueJsonAsync(SiteContentKey key, CancellationToken ct);

    /// <summary>Insert-or-update, saves internally.</summary>
    Task UpsertAsync(SiteContentKey key, string valueJson, CancellationToken ct);
}
