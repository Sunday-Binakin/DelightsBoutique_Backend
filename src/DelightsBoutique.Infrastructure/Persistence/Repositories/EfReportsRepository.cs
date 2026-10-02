using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Reports.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfReportsRepository(AppDbContext db) : IReportsRepository
{
    private IQueryable<Order> FilteredOrders(ReportsFilter filter)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();

        if (filter.From is not null)
            query = query.Where(o => o.CreatedAt >= filter.From);

        if (filter.To is not null)
            query = query.Where(o => o.CreatedAt <= filter.To);

        if (filter.Fulfillment is not null)
            query = query.Where(o => o.Fulfillment == filter.Fulfillment);

        if (filter.PaymentMethod is not null)
            query = query.Where(o => o.PaymentMethod == filter.PaymentMethod);

        // Order items snapshot `slug`, not `categorySlug` — same join-through-Products
        // idiom as EfOrderRepository.SearchAsync's admin order list filter.
        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            query = query.Where(o => o.Items.Any(i =>
                db.Products.Any(p => p.Slug == i.Slug && p.CategorySlug == filter.CategorySlug)));

        return query;
    }

    public async Task<ReportsSummaryDto> GetSummaryAsync(ReportsFilter filter, CancellationToken ct)
    {
        var orders = FilteredOrders(filter);

        var totalOrders = await orders.CountAsync(ct);
        var cancelledOrders = await orders.CountAsync(o => o.Status == OrderStatus.Cancelled, ct);
        var confirmedRevenue = await orders.Where(o => o.Status != OrderStatus.Cancelled).SumAsync(o => o.Subtotal, ct);

        return new ReportsSummaryDto(totalOrders, cancelledOrders, confirmedRevenue);
    }

    public Task<List<StatusBreakdownEntry>> GetStatusBreakdownAsync(ReportsFilter filter, CancellationToken ct) =>
        FilteredOrders(filter)
            .GroupBy(o => o.Status)
            .Select(g => new StatusBreakdownEntry(g.Key, g.Count()))
            .ToListAsync(ct);

    public Task<List<PaymentBreakdownEntry>> GetPaymentBreakdownAsync(ReportsFilter filter, CancellationToken ct) =>
        FilteredOrders(filter)
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new PaymentBreakdownEntry(g.Key, g.Count()))
            .ToListAsync(ct);

    public Task<List<TopProductDto>> GetTopProductsAsync(ReportsFilter filter, CancellationToken ct)
    {
        var filteredOrderIds = FilteredOrders(filter).Where(o => o.Status != OrderStatus.Cancelled).Select(o => o.Id);

        // Anonymous-type projection first (same idiom as EfProductRepository.SearchAsync),
        // OrderBy/Take on that shape, and only the final .Select() projects into the named
        // DTO — never filter/order after a Select into a constructed record (see
        // EfCustomerRepository's Phase 4 bug).
        return db.OrderItems.AsNoTracking()
            .Where(i => filteredOrderIds.Contains(i.OrderId))
            .GroupBy(i => i.Slug)
            .Select(g => new { Slug = g.Key, Name = g.Max(i => i.Name)!, UnitsSold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Quantity * i.UnitPrice) }) // group is never empty by construction, so Max(Name) is never actually null
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .Select(x => new TopProductDto(x.Slug, x.Name, x.UnitsSold, x.Revenue))
            .ToListAsync(ct);
    }
}
