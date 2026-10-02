namespace DelightsBoutique.Application.Abstractions;

public interface IAuditLogRepository
{
    Task AddAsync(string actor, string action, CancellationToken ct);
}
