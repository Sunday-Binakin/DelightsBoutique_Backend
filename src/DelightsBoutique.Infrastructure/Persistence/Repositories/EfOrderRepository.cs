using System.Linq.Expressions;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
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

    public Task<AdminOrderDto?> GetAdminDtoByIdAsync(string id, CancellationToken ct) =>
        db.Orders.AsNoTracking().Where(o => o.Id == id).Select(ToAdminDto).FirstOrDefaultAsync(ct);

    public Task<List<OrderDto>> GetByPhoneAsync(string normalizedPhone, CancellationToken ct) =>
        db.Orders.AsNoTracking().Where(o => o.Phone == normalizedPhone)
            .OrderByDescending(o => o.CreatedAt).Select(ToDto).ToListAsync(ct);

    public async Task<PagedResult<AdminOrderDto>> SearchAsync(OrderSearchFilter filter, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();

        if (filter.Status is not null)
            query = query.Where(o => o.Status == filter.Status);

        if (filter.Fulfillment is not null)
            query = query.Where(o => o.Fulfillment == filter.Fulfillment);

        if (filter.PaymentMethod is not null)
            query = query.Where(o => o.PaymentMethod == filter.PaymentMethod);

        if (filter.From is not null)
            query = query.Where(o => o.CreatedAt >= filter.From);

        if (filter.To is not null)
            query = query.Where(o => o.CreatedAt <= filter.To);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(o =>
                EF.Functions.ILike(o.CustomerName, $"%{filter.Search}%") ||
                EF.Functions.ILike(o.Phone, $"%{filter.Search}%"));

        // Order items snapshot `slug`, not `categorySlug` — join through Products to resolve.
        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            query = query.Where(o => o.Items.Any(i =>
                db.Products.Any(p => p.Slug == i.Slug && p.CategorySlug == filter.CategorySlug)));

        query = query.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id); // ThenBy stabilizes paging across ties

        // Deliberately two round trips — see EfProductRepository.SearchAsync for the same idiom.
        var total = await query.CountAsync(ct);

        var data = await query
            .Skip((filter.Page - 1) * filter.Limit)
            .Take(filter.Limit)
            .Select(ToAdminDto)
            .ToListAsync(ct);

        return new PagedResult<AdminOrderDto>(data, filter.Page, filter.Limit, total);
    }

    // DTO-projecting queries use .Select(ToDto) directly, without .Include — EF Core
    // translates the nested Items.Select(...) projection on its own; combining .Include
    // with a terminal .Select projection is redundant and can emit an EF warning.
    private static readonly Expression<Func<Order, OrderDto>> ToDto = o => new OrderDto(
        o.Id, o.CreatedAt, o.Status,
        o.Items.Select(i => new OrderItemDto(i.Slug, i.Name, i.Quantity, i.UnitPrice, i.Size, i.Color)).ToList(),
        o.Subtotal, o.CustomerName, o.Phone, o.Fulfillment, o.Address, o.PaymentMethod, o.PaymentProofUrl, o.Notes);

    private static readonly Expression<Func<Order, AdminOrderDto>> ToAdminDto = o => new AdminOrderDto(
        o.Id, o.CreatedAt, o.Status,
        o.Items.Select(i => new OrderItemDto(i.Slug, i.Name, i.Quantity, i.UnitPrice, i.Size, i.Color)).ToList(),
        o.Subtotal, o.CustomerName, o.Phone, o.Fulfillment, o.Address, o.PaymentMethod, o.PaymentProofUrl, o.Notes, o.InternalNotes);
}
