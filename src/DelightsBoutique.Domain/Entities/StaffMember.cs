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

    public static StaffMember Create(string name, string email, string? phone, List<Permission> permissions, string passwordHash) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email,
        Phone = phone,
        Permissions = permissions,
        Active = true,
        PasswordHash = passwordHash,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Every parameter is "new value, or null/omitted to leave unchanged" — same
    /// partial-update convention as Product.Update.</summary>
    public void UpdateProfile(string? name, string? email, string? phone, List<Permission>? permissions, bool? active)
    {
        if (name is not null) Name = name;
        if (email is not null) Email = email;
        if (phone is not null) Phone = phone;
        if (permissions is not null) Permissions = permissions;
        if (active is not null) Active = active.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Test-support only.</summary>
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
