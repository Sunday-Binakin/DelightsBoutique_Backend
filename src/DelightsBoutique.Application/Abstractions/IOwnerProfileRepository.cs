using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IOwnerProfileRepository
{
    Task<OwnerProfile?> GetByEmailAsync(string email, CancellationToken ct);
    Task<OwnerProfile?> GetByIdAsync(Guid id, CancellationToken ct);
}
