using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Audit.Dtos;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfAuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(string actor, string action, CancellationToken ct)
    {
        db.AuditEntries.Add(AuditEntry.Create(actor, action));
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditEntryDto>> SearchAsync(AuditLogSearchFilter filter, CancellationToken ct)
    {
        var query = db.AuditEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(e =>
                EF.Functions.ILike(e.Actor, $"%{filter.Search}%") ||
                EF.Functions.ILike(e.Action, $"%{filter.Search}%"));

        query = query.OrderByDescending(e => e.Timestamp).ThenBy(e => e.Id); // ThenBy stabilizes paging across ties

        // Deliberately two round trips — see EfProductRepository.SearchAsync for the same idiom.
        var total = await query.CountAsync(ct);

        var data = await query
            .Skip((filter.Page - 1) * filter.Limit)
            .Take(filter.Limit)
            .Select(e => new AuditEntryDto(e.Id, e.Timestamp, e.Actor, e.Action))
            .ToListAsync(ct);

        return new PagedResult<AuditEntryDto>(data, filter.Page, filter.Limit, total);
    }
}
