using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfCustomerFlagRepository(AppDbContext db) : ICustomerFlagRepository
{
    public Task<CustomerFlag?> GetAsync(string normalizedPhone, CancellationToken ct) =>
        db.CustomerFlags.AsNoTracking().FirstOrDefaultAsync(f => f.PhoneNormalized == normalizedPhone, ct);

    public async Task UpsertAsync(string normalizedPhone, bool wholesale, CancellationToken ct)
    {
        var flag = await db.CustomerFlags.FirstOrDefaultAsync(f => f.PhoneNormalized == normalizedPhone, ct);
        if (flag is null)
            db.CustomerFlags.Add(CustomerFlag.Create(normalizedPhone, wholesale));
        else
            flag.SetWholesale(wholesale);

        await db.SaveChangesAsync(ct);
    }
}
