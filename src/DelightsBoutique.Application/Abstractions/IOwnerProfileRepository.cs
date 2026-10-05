using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IOwnerProfileRepository
{
    Task<OwnerProfile?> GetByEmailAsync(string email, CancellationToken ct);
    Task<OwnerProfile?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Entity already mutated by the caller; saves internally.</summary>
    Task UpdateAsync(OwnerProfile owner, CancellationToken ct);
}
