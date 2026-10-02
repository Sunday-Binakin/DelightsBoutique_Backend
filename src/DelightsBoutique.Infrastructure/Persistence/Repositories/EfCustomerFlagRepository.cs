using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfCustomerFlagRepository(AppDbContext db) : ICustomerFlagRepository
{
    public Task<CustomerFlag?> GetAsync(string normalizedPhone, CancellationToken ct) =>
        db.CustomerFlags.AsNoTracking().FirstOrDefaultAsync(f => f.PhoneNormalized == normalizedPhone, ct);
}
