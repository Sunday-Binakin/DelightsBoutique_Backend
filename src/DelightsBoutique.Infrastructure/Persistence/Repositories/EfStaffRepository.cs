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

    public Task<List<StaffMember>> GetAllAsync(CancellationToken ct) =>
        db.StaffMembers.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken ct) =>
        db.StaffMembers.AnyAsync(s => s.Email == email.ToLowerInvariant() && s.Id != excludingId, ct);

    public async Task AddAsync(StaffMember staff, CancellationToken ct)
    {
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(StaffMember staff, CancellationToken ct) => db.SaveChangesAsync(ct);
}
