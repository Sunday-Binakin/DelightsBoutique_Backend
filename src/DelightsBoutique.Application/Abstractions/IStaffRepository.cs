using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IStaffRepository
{
    Task<StaffMember?> GetByEmailAsync(string email, CancellationToken ct);
    Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Plain list, not paginated — staff rosters are small and the spec defines no
    /// query params for this endpoint.</summary>
    Task<List<StaffMember>> GetAllAsync(CancellationToken ct);

    /// <summary>excludingId lets an update keep a staff member's own existing email
    /// without tripping a false "duplicate" against itself.</summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken ct);

    Task AddAsync(StaffMember staff, CancellationToken ct);

    /// <summary>Entity already mutated by the caller; saves internally.</summary>
    Task UpdateAsync(StaffMember staff, CancellationToken ct);
}
