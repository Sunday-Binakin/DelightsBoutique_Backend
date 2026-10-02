using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);

    /// <summary>Tracked, Items included — for payment-proof mutation.</summary>
    Task<Order?> GetEntityByIdAsync(string id, CancellationToken ct);

    /// <summary>Entity already mutated by the caller; saves internally.</summary>
    Task UpdateAsync(Order order, CancellationToken ct);

    Task<OrderDto?> GetDtoByIdAsync(string id, CancellationToken ct);

    /// <summary>Newest-first.</summary>
    Task<List<OrderDto>> GetByPhoneAsync(string normalizedPhone, CancellationToken ct);
}
