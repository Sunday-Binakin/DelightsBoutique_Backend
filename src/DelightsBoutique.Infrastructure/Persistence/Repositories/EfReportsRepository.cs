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
        var pendingAmount = await orders.Where(o => o.Status == OrderStatus.PendingConfirmation).SumAsync(o => o.Subtotal, ct);
        var cancelledAmount = await orders.Where(o => o.Status == OrderStatus.Cancelled).SumAsync(o => o.Subtotal, ct);

        return new ReportsSummaryDto(totalOrders, cancelledOrders, confirmedRevenue, pendingAmount, cancelledAmount);
    }

    public Task<List<StatusBreakdownEntry>> GetStatusBreakdownAsync(ReportsFilter filter, CancellationToken ct) =>
        FilteredOrders(filter)
            .GroupBy(o => o.Status)
            .Select(g => new StatusBreakdownEntry(g.Key, g.Count(), g.Sum(o => o.Subtotal)))
            .ToListAsync(ct);

    public Task<List<PaymentBreakdownEntry>> GetPaymentBreakdownAsync(ReportsFilter filter, CancellationToken ct) =>
        FilteredOrders(filter)
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new PaymentBreakdownEntry(g.Key, g.Count(), g.Sum(o => o.Subtotal)))
            .ToListAsync(ct);

    public async Task<List<DailyRevenueEntry>> GetDailySeriesAsync(ReportsFilter filter, CancellationToken ct)
    {
        // Grouped by DateTime.Date server-side (translates fine via Npgsql); the DateOnly
        // conversion happens client-side afterward, since constructing a DateOnly directly
        // inside a translated GroupBy/Select is not reliably supported across providers.
        var raw = await FilteredOrders(filter)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, OrderCount = g.Count(), Revenue = g.Sum(o => o.Subtotal) })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return raw.Select(x => new DailyRevenueEntry(DateOnly.FromDateTime(x.Date), x.OrderCount, x.Revenue)).ToList();
    }

    public Task<List<TopProductDto>> GetTopProductsAsync(ReportsFilter filter, CancellationToken ct)
    {
        var filteredOrderIds = FilteredOrders(filter).Where(o => o.Status != OrderStatus.Cancelled).Select(o => o.Id);

        // Anonymous-type projection first (same idiom as EfProductRepository.SearchAsync),
        // OrderBy/Take on that shape, and only the final .Select() projects into the named
        // DTO — never filter/order after a Select into a constructed record (see
        // EfCustomerRepository's Phase 4 bug).
        var grouped = db.OrderItems.AsNoTracking()
            .Where(i => filteredOrderIds.Contains(i.OrderId))
            .GroupBy(i => i.Slug)
            .Select(g => new { Slug = g.Key, Name = g.Max(i => i.Name)!, UnitsSold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Quantity * i.UnitPrice) }); // group is never empty by construction, so Max(Name) is never actually null

        // Left-joined through Products to Categories, same "product/category may no longer
        // exist" tolerance as EfProductRepository's own joins — an order item snapshots its
        // own slug/name independent of the product still existing.
        var withCategory =
            from x in grouped
            join p in db.Products.AsNoTracking() on x.Slug equals p.Slug into pj
            from p in pj.DefaultIfEmpty()
            join c in db.Categories.AsNoTracking() on (p != null ? p.CategorySlug : null) equals c.Slug into cj
            from c in cj.DefaultIfEmpty()
            select new { x.Slug, x.Name, x.UnitsSold, x.Revenue, Category = c != null ? c.Name : (p != null ? p.CategorySlug : null) };

        return withCategory
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .Select(x => new TopProductDto(x.Slug, x.Name, x.UnitsSold, x.Revenue, x.Category))
            .ToListAsync(ct);
    }
}
