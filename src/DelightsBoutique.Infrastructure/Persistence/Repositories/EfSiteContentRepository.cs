using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfSiteContentRepository(AppDbContext db) : ISiteContentRepository
{
    public Task<string?> GetValueJsonAsync(SiteContentKey key, CancellationToken ct) =>
        db.SiteContents.AsNoTracking().Where(s => s.Key == key).Select(s => s.ValueJson).FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(SiteContentKey key, string valueJson, CancellationToken ct)
    {
        var entity = await db.SiteContents.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (entity is null)
            db.SiteContents.Add(SiteContent.Create(key, valueJson));
        else
            entity.UpdateValue(valueJson);

        await db.SaveChangesAsync(ct);
    }
}
