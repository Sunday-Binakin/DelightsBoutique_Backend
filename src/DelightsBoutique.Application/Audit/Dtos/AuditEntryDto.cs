namespace DelightsBoutique.Application.Audit.Dtos;

public record AuditEntryDto(Guid Id, DateTime Timestamp, string Actor, string Action);

public record AuditLogSearchFilter(string? Search, int Page, int Limit);
