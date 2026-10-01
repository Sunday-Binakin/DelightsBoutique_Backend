using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Domain.Entities;

public class StaffMember
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public List<Permission> Permissions { get; private set; } = [];
    public bool Active { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected StaffMember() { }

    /// <summary>Test-support only, until Phase 4's real staff-creation handler exists.</summary>
    internal static StaffMember ForTesting(string email, string passwordHash, bool active, List<Permission>? permissions = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Staff",
        Email = email,
        Permissions = permissions ?? [],
        Active = active,
        PasswordHash = passwordHash,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
