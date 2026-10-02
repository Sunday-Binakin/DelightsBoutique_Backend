using DelightsBoutique.Application.Abstractions;
using MediatR;

namespace DelightsBoutique.Application.Common;

/// <summary>
/// Writes an AuditEntry after any command implementing <see cref="IAuditableCommand"/>
/// succeeds. Registered after ValidationBehavior, so a validation failure never reaches
/// this behavior's await next() — and a handler exception equally means the audit write
/// (which happens after next() returns) never runs. The audit write is a separate
/// SaveChangesAsync call from the handler's own, immediately following it — not a literal
/// shared transaction (a deliberate, documented KISS gap, see BUILD_SPEC §9).
/// </summary>
public class AuditLoggingBehavior<TRequest, TResponse>(IAuditLogRepository auditLog, ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableCommand auditable)
        {
            var actor = currentUser.Email ?? currentUser.Name ?? "unknown";
            await auditLog.AddAsync(actor, auditable.DescribeAction(), cancellationToken);
        }

        return response;
    }
}
