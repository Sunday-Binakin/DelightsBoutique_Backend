using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfAuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(string actor, string action, CancellationToken ct)
    {
        db.AuditEntries.Add(AuditEntry.Create(actor, action));
        await db.SaveChangesAsync(ct);
    }
}
