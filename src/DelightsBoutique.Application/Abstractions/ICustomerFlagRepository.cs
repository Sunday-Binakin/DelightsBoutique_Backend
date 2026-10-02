using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface ICustomerFlagRepository
{
    /// <summary>Null = not flagged wholesale. No write path this phase — the admin
    /// "flag customer wholesale" endpoint is Phase 4's (Manage Customers).</summary>
    Task<CustomerFlag?> GetAsync(string normalizedPhone, CancellationToken ct);
}
