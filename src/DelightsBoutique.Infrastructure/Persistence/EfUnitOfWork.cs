using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Infrastructure.Persistence;

public class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await action(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
