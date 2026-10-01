using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Auth.Dtos;

public abstract record VerifyOtpResult;

public record VerifyOtpSucceeded(
    Guid UserId,
    string Role, // "Owner" | "Staff"
    Guid? StaffId,
    string Email,
    string Name,
    IReadOnlyList<Permission> Permissions) : VerifyOtpResult;

public record VerifyOtpFailed : VerifyOtpResult;
