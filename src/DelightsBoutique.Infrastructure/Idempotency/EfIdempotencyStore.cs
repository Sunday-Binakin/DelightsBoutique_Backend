using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Idempotency;

public class EfIdempotencyStore(AppDbContext db, IClock clock) : IIdempotencyStore
{
    public async Task<IdempotencyKeyLookup?> FindAsync(string key, string endpoint, CancellationToken ct)
    {
        var record = await db.IdempotencyKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == key && k.Endpoint == endpoint, ct);

        return record is null ? null : new IdempotencyKeyLookup(record.RequestHash, record.ResponseStatus, record.ResponseBody);
    }

    public async Task SaveAsync(string key, string endpoint, string requestHash, int responseStatus, string responseBody, CancellationToken ct)
    {
        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Id = Guid.NewGuid(),
            Key = key,
            Endpoint = endpoint,
            RequestHash = requestHash,
            ResponseStatus = responseStatus,
            ResponseBody = responseBody,
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
