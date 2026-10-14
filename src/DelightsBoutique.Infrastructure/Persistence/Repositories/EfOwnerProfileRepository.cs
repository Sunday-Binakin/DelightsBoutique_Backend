using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfOwnerProfileRepository(AppDbContext db) : IOwnerProfileRepository
{
    public Task<OwnerProfile?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.OwnerProfiles.FirstOrDefaultAsync(o => o.Email == email.ToLowerInvariant(), ct);

    public Task<OwnerProfile?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.OwnerProfiles.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task UpdateAsync(OwnerProfile owner, CancellationToken ct) => db.SaveChangesAsync(ct);
}
