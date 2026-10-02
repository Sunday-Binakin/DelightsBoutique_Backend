namespace DelightsBoutique.Application.Abstractions;

/// <summary>Pure data, no I/O — the implementation (reading HttpContext claims) lives in Api.</summary>
public interface ICurrentUser
{
    string? Email { get; }
    string? Name { get; }
    string? Role { get; } // "Owner" | "Staff" | null
    Guid? StaffId { get; }
}
