using DelightsBoutique.Application.Audit.Dtos;
using DelightsBoutique.Application.Common;

namespace DelightsBoutique.Application.Abstractions;

public interface IAuditLogRepository
{
    Task AddAsync(string actor, string action, CancellationToken ct);

    Task<PagedResult<AuditEntryDto>> SearchAsync(AuditLogSearchFilter filter, CancellationToken ct);
}
