using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);

    /// <summary>Tracked, Items included — for payment-proof mutation and admin status/notes updates.</summary>
    Task<Order?> GetEntityByIdAsync(string id, CancellationToken ct);

    /// <summary>Entity already mutated by the caller; saves internally. This is also where
    /// Order's Postgres xmin concurrency check fires on a stale write.</summary>
    Task UpdateAsync(Order order, CancellationToken ct);

    Task<OrderDto?> GetDtoByIdAsync(string id, CancellationToken ct);

    /// <summary>Admin view — includes InternalNotes.</summary>
    Task<AdminOrderDto?> GetAdminDtoByIdAsync(string id, CancellationToken ct);

    /// <summary>Newest-first.</summary>
    Task<List<OrderDto>> GetByPhoneAsync(string normalizedPhone, CancellationToken ct);

    Task<PagedResult<AdminOrderDto>> SearchAsync(OrderSearchFilter filter, CancellationToken ct);
}
