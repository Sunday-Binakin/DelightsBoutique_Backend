using System.Linq.Expressions;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfOrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public Task<Order?> GetEntityByIdAsync(string id, CancellationToken ct) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task UpdateAsync(Order order, CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<OrderDto?> GetDtoByIdAsync(string id, CancellationToken ct) =>
        db.Orders.AsNoTracking().Where(o => o.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);

    public Task<List<OrderDto>> GetByPhoneAsync(string normalizedPhone, CancellationToken ct) =>
        db.Orders.AsNoTracking().Where(o => o.Phone == normalizedPhone)
            .OrderByDescending(o => o.CreatedAt).Select(ToDto).ToListAsync(ct);

    // DTO-projecting queries use .Select(ToDto) directly, without .Include — EF Core
    // translates the nested Items.Select(...) projection on its own; combining .Include
    // with a terminal .Select projection is redundant and can emit an EF warning.
    private static readonly Expression<Func<Order, OrderDto>> ToDto = o => new OrderDto(
        o.Id, o.CreatedAt, o.Status,
        o.Items.Select(i => new OrderItemDto(i.Slug, i.Name, i.Quantity, i.UnitPrice, i.Size, i.Color)).ToList(),
        o.Subtotal, o.CustomerName, o.Phone, o.Fulfillment, o.Address, o.PaymentMethod, o.PaymentProofUrl, o.Notes);
}
