using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Audit.Dtos;
using DelightsBoutique.Application.Common;
using MediatR;

namespace DelightsBoutique.Application.Audit.Queries;

public record GetAuditLogQuery(string? Search, int Page = 1, int Limit = 20) : IRequest<PagedResult<AuditEntryDto>>;

public class GetAuditLogQueryHandler(IAuditLogRepository auditLog) : IRequestHandler<GetAuditLogQuery, PagedResult<AuditEntryDto>>
{
    public Task<PagedResult<AuditEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken) =>
        auditLog.SearchAsync(new AuditLogSearchFilter(request.Search, request.Page, request.Limit), cancellationToken);
}
