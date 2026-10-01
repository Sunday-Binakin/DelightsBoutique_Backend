using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IStaffRepository
{
    Task<StaffMember?> GetByEmailAsync(string email, CancellationToken ct);
    Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken ct);
}
