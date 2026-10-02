namespace DelightsBoutique.Application.Abstractions;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}
