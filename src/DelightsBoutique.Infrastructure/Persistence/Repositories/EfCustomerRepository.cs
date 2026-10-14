using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Customers.Dtos;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfCustomerRepository(AppDbContext db) : ICustomerRepository
{
    // Step 1: pure aggregates per phone — Count/Sum/Max all translate cleanly as a GroupBy.
    // Step 2: self-join back to Orders on (Phone, LastOrderAt) to pick that order's
    // CustomerName — a plain equi-join translates far more reliably than a correlated
    // "First() inside a GroupBy projection."
    // Deliberately NOT projected into CustomerDto/any record type here — an anonymous-type
    // projection, same as EfProductRepository.SearchAsync. Filtering with .Where() AFTER a
    // .Select() into a constructed record type fails to translate once this many
    // GroupBy/Join/LeftJoin stages are already composed (confirmed empirically: EF throws
    // "could not be translated" on the final predicate). Every .Where() below must run
    // against this anonymous shape; the one-and-only .Select(... new CustomerDto(...)) is
    // always the last thing in the chain.
    private IQueryable<CustomerRow> BaseQuery() =>
        from a in
            (from o in db.Orders.AsNoTracking()
             group o by o.Phone into g
             select new
             {
                 Phone = g.Key,
                 OrderCount = g.Count(),
                 // Same "excludes Cancelled" predicate as the spec's confirmedRevenue — one
                 // shared definition of "spent," not two that can drift.
                 TotalSpent = g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Subtotal),
                 LastOrderAt = g.Max(o => o.CreatedAt)
             })
        join o in db.Orders.AsNoTracking() on new { a.Phone, a.LastOrderAt } equals new { o.Phone, LastOrderAt = o.CreatedAt }
        join f in db.CustomerFlags.AsNoTracking() on a.Phone equals f.PhoneNormalized into fj
        from f in fj.DefaultIfEmpty()
        select new CustomerRow(a.Phone, o.CustomerName, a.OrderCount, a.TotalSpent, a.LastOrderAt, f != null && f.Wholesale);

    private record CustomerRow(string Phone, string Name, int OrderCount, decimal TotalSpent, DateTime LastOrderAt, bool Wholesale);

    public async Task<CustomerDto?> GetByPhoneAsync(string normalizedPhone, CancellationToken ct)
    {
        var rows = await BaseQuery().ToListAsync(ct);
        return rows.Where(x => x.Phone == normalizedPhone)
            .Select(x => new CustomerDto(x.Phone, x.Name, x.OrderCount, x.TotalSpent, x.LastOrderAt, x.Wholesale))
            .FirstOrDefault();
    }

    public async Task<PagedResult<CustomerDto>> SearchAsync(CustomerSearchFilter filter, CancellationToken ct)
    {
        // The whole aggregation (GroupBy + two joins) is pulled into memory once, then
        // filtered/paged client-side. Acceptable for this read model: the customer count is
        // bounded by distinct phone numbers that have ever ordered, not raw order volume,
        // and the alternative — pushing Where() through this many composed EF stages — is
        // exactly what fails to translate (see BaseQuery's comment).
        var all = await BaseQuery().ToListAsync(ct);

        IEnumerable<CustomerRow> query = all;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search;
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // CustomerType wins when both are given — see CustomerSearchFilter's own comment.
        var effectiveType = filter.CustomerType ?? (filter.WholesaleOnly == true ? CustomerType.Wholesale : (CustomerType?)null);
        if (effectiveType == CustomerType.Wholesale)
            query = query.Where(x => x.Wholesale);
        else if (effectiveType == CustomerType.Retail)
            query = query.Where(x => !x.Wholesale);

        var ordered = query.OrderByDescending(x => x.LastOrderAt).ThenBy(x => x.Phone).ToList(); // ThenBy stabilizes paging across ties
        var total = ordered.Count;

        var data = ordered
            .Skip((filter.Page - 1) * filter.Limit)
            .Take(filter.Limit)
            .Select(x => new CustomerDto(x.Phone, x.Name, x.OrderCount, x.TotalSpent, x.LastOrderAt, x.Wholesale))
            .ToList();

        return new PagedResult<CustomerDto>(data, filter.Page, filter.Limit, total);
    }

    public async Task<CustomerCountsDto> GetCountsAsync(string? search, CancellationToken ct)
    {
        var all = await BaseQuery().ToListAsync(ct);

        IEnumerable<CustomerRow> query = all;
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase));

        var matching = query.ToList();
        var wholesaleCount = matching.Count(x => x.Wholesale);
        return new CustomerCountsDto(matching.Count, matching.Count - wholesaleCount, wholesaleCount);
    }
}
