using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Staff.Dtos;

// Never PasswordHash — structurally cannot leak it, matches the public/admin DTO precedent used elsewhere.
public record StaffDto(Guid Id, string Name, string Email, string? Phone, List<Permission> Permissions, bool Active, DateTime CreatedAt, DateTime UpdatedAt);

public record CreateStaffBody(string Name, string Email, string? Phone, List<Permission> Permissions);

public record CreateStaffResult(StaffDto Member, string Password);

public record UpdateStaffBody(string? Name, string? Email, string? Phone, List<Permission>? Permissions, bool? Active);

public record ResetPasswordResult(string Password);
