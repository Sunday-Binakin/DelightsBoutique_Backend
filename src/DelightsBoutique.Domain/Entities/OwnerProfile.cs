namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// Singleton — exactly one row. Nothing in the API ever creates a second owner
/// (there is no "create owner" endpoint anywhere in the spec), so this is enforced
/// by convention (the seeder's own empty-table check) rather than a DB constraint.
/// </summary>
public class OwnerProfile
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string PasswordHash { get; private set; } = default!;

    protected OwnerProfile() { }

    public static OwnerProfile Create(string name, string email, string? phone, string passwordHash) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email,
        Phone = phone,
        PasswordHash = passwordHash
    };
}
