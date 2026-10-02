using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Customers.Dtos;

namespace DelightsBoutique.Application.Abstractions;

/// <summary>Customers aren't a stored entity — they're an aggregation over Orders (grouped
/// by phone) plus CustomerFlags, computed at query time per BUILD_SPEC §4 ("one source of
/// truth, nothing to drift"). This is a read-model repository, not an entity repository.</summary>
public interface ICustomerRepository
{
    Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchFilter filter, CancellationToken ct);

    /// <summary>Null if this phone has no orders — "customer" is defined as an aggregation
    /// over Orders, so a phone with zero orders isn't a customer yet, regardless of any
    /// CustomerFlag row that might exist for it.</summary>
    Task<CustomerDto?> GetByPhoneAsync(string normalizedPhone, CancellationToken ct);
}
