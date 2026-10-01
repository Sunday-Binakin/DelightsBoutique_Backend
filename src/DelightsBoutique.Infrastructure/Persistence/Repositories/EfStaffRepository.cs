using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfStaffRepository(AppDbContext db) : IStaffRepository
{
    public Task<StaffMember?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.StaffMembers.FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant(), ct);

    public Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, ct);
}
