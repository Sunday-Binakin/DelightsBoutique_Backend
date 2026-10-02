using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface ICustomerFlagRepository
{
    /// <summary>Null = not flagged wholesale.</summary>
    Task<CustomerFlag?> GetAsync(string normalizedPhone, CancellationToken ct);

    /// <summary>Load-or-create, saves internally.</summary>
    Task UpsertAsync(string normalizedPhone, bool wholesale, CancellationToken ct);
}
