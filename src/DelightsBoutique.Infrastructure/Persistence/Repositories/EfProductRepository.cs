using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfProductRepository(AppDbContext db) : IProductRepository
{
    public Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken ct) =>
        (from p in db.Products.AsNoTracking()
         join c in db.Categories.AsNoTracking() on p.CategorySlug equals c.Slug into cj
         from c in cj.DefaultIfEmpty()
         where p.Slug == slug
         select new ProductDto(p.Id, p.Slug, p.Name, p.CategorySlug, c != null ? c.Name : p.CategorySlug,
             p.Description, p.RetailPrice, p.WholesalePrice, p.WholesaleMoq, p.CompareAtPrice,
             p.Rating, p.ReviewCount, p.Tone, p.Badge, p.StockStatus,
             p.Sizes, p.Colors, p.ImageUrls, p.CreatedAt, p.UpdatedAt))
        .FirstOrDefaultAsync(ct);

    public Task<Product?> GetEntityBySlugAsync(string slug, CancellationToken ct) =>
        db.Products.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    // NOTE: this filter-building block is deliberately duplicated in GetFacetsAsync below,
    // not shared via a helper returning a named row type — tried that once (a private
    // record instead of the anonymous type), and confirmed live against the real database
    // that EF Core's translator breaks on it the same way it breaks on a named ValueTuple
    // (same class of bug this comment already warned about): "could not be translated"
    // once further Where/OrderBy/SelectMany composes against the named type's properties.
    // Anonymous types are the only form of this intermediate row confirmed to survive that
    // composition, and an anonymous type can't be a shared helper's declared return type.
    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchFilter filter, CancellationToken ct)
    {
        var query =
            from p in db.Products.AsNoTracking()
            join c in db.Categories.AsNoTracking() on p.CategorySlug equals c.Slug into cj
            from c in cj.DefaultIfEmpty()
            select new { p, CategoryName = c != null ? c.Name : p.CategorySlug };

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            query = query.Where(x => x.p.CategorySlug == filter.CategorySlug);

        // Multi-word AND: each word independently must appear (as a substring) in Name,
        // Description, or the product's category name — successive .Where() calls compose
        // with AND, so "red dress" requires "red" somewhere AND "dress" somewhere, not the
        // literal two-word phrase. A blank/whitespace-only term is already handled by the
        // outer IsNullOrWhiteSpace check — it skips this filter entirely and returns the
        // catalog unfiltered.
        var searchWords = string.IsNullOrWhiteSpace(filter.Search)
            ? []
            : filter.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var word in searchWords)
        {
            var pattern = $"%{word}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.p.Name, pattern) ||
                EF.Functions.ILike(x.p.Description, pattern) ||
                EF.Functions.ILike(x.CategoryName, pattern));
        }

        if (filter.Badge is not null)
            query = query.Where(x => x.p.Badge == filter.Badge);

        if (filter.MinPrice is not null)
            query = query.Where(x => x.p.RetailPrice >= filter.MinPrice);

        if (filter.MaxPrice is not null)
            query = query.Where(x => x.p.RetailPrice <= filter.MaxPrice);

        // Product has ANY of the requested sizes/colors — the product's own text[] column
        // is the outer .Any(), checked against the (small, client-supplied) requested list,
        // not the other way around: this direction translates reliably against a Postgres
        // array column, whereas "requestedList.Any(x => product.Sizes.Contains(x))" risks
        // EF Core not finding a translation for a C#-side list driving an array-column check.
        if (filter.Sizes is { Count: > 0 })
            query = query.Where(x => x.p.Sizes.Any(s => filter.Sizes.Contains(s)));

        if (filter.Colors is { Count: > 0 })
            query = query.Where(x => x.p.Colors.Any(c => filter.Colors.Contains(c)));

        // Name-match-first: when searching, a product whose Name contains the full search
        // phrase outranks one that only matched via Description (or via Name/Description
        // individually containing different words) — the requested Sort then only breaks
        // ties within each of those two relevance tiers, rather than ignoring relevance.
        var ordered = searchWords.Length > 0
            ? query.OrderByDescending(x => EF.Functions.ILike(x.p.Name, $"%{filter.Search}%"))
            : query.OrderBy(x => 0); // no search term — no relevance tier, Sort alone decides order

        query = filter.Sort switch
        {
            "price-asc" => ordered.ThenBy(x => x.p.RetailPrice).ThenBy(x => x.p.Id),
            "price-desc" => ordered.ThenByDescending(x => x.p.RetailPrice).ThenBy(x => x.p.Id),
            "rating" => ordered.ThenByDescending(x => x.p.Rating).ThenBy(x => x.p.Id),
            _ => ordered.ThenByDescending(x => x.p.CreatedAt).ThenBy(x => x.p.Id) // "newest" default; ThenBy stabilizes paging across ties
        };

        // Deliberately two round trips, not one: EF Core has no clean LINQ shape for
        // "paged rows + accurate total" without a raw OVER() window function, which both
        // BUILD_SPEC's no-raw-SQL rule and KISS argue against for a catalog this size.
        var total = await query.CountAsync(ct);

        var data = await query
            .Skip((filter.Page - 1) * filter.Limit)
            .Take(filter.Limit)
            .Select(x => new ProductDto(x.p.Id, x.p.Slug, x.p.Name, x.p.CategorySlug, x.CategoryName,
                x.p.Description, x.p.RetailPrice, x.p.WholesalePrice, x.p.WholesaleMoq, x.p.CompareAtPrice,
                x.p.Rating, x.p.ReviewCount, x.p.Tone, x.p.Badge, x.p.StockStatus,
                x.p.Sizes, x.p.Colors, x.p.ImageUrls, x.p.CreatedAt, x.p.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(data, filter.Page, filter.Limit, total);
    }

    public async Task<ProductFacetsDto> GetFacetsAsync(ProductFacetsFilter filter, CancellationToken ct)
    {
        var query =
            from p in db.Products.AsNoTracking()
            join c in db.Categories.AsNoTracking() on p.CategorySlug equals c.Slug into cj
            from c in cj.DefaultIfEmpty()
            select new { p, CategoryName = c != null ? c.Name : p.CategorySlug };

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            query = query.Where(x => x.p.CategorySlug == filter.CategorySlug);

        var searchWords = string.IsNullOrWhiteSpace(filter.Search)
            ? []
            : filter.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var word in searchWords)
        {
            var pattern = $"%{word}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.p.Name, pattern) ||
                EF.Functions.ILike(x.p.Description, pattern) ||
                EF.Functions.ILike(x.CategoryName, pattern));
        }

        if (filter.Badge is not null)
            query = query.Where(x => x.p.Badge == filter.Badge);

        if (filter.MinPrice is not null)
            query = query.Where(x => x.p.RetailPrice >= filter.MinPrice);

        if (filter.MaxPrice is not null)
            query = query.Where(x => x.p.RetailPrice <= filter.MaxPrice);

        // No Sizes/Colors/Sort/paging applied here, deliberately — this reports every
        // size/color option the scope has, so it must stay unnarrowed by the very
        // dimension it's reporting on.
        //
        // SelectMany(x => x.p.Sizes) to unnest the array server-side does NOT translate
        // here (confirmed live: "could not be translated" on EF.Property<List<string>>,
        // even though .Any() against the same column translates fine elsewhere in this
        // file) — flattening/distinct-ing a Postgres array column via SelectMany is a
        // different operation than membership-checking it, and this EF Core/Npgsql
        // combination only supports the latter in a composed query. Materializing the
        // (filter-bounded) Sizes/Colors arrays first and flattening in memory sidesteps it.
        var rows = await query.Select(x => new { x.p.Sizes, x.p.Colors }).ToListAsync(ct);
        var sizes = rows.SelectMany(r => r.Sizes).Distinct().OrderBy(s => s).ToList();
        var colors = rows.SelectMany(r => r.Colors).Distinct().OrderBy(c => c).ToList();

        return new ProductFacetsDto(sizes, colors);
    }

    public async Task<List<ProductDto>> GetBySlugsAsync(IReadOnlyList<string> slugs, CancellationToken ct)
    {
        if (slugs.Count == 0) return [];

        var found = await (from p in db.Products.AsNoTracking()
                            join c in db.Categories.AsNoTracking() on p.CategorySlug equals c.Slug into cj
                            from c in cj.DefaultIfEmpty()
                            where slugs.Contains(p.Slug)
                            select new ProductDto(p.Id, p.Slug, p.Name, p.CategorySlug, c != null ? c.Name : p.CategorySlug,
                                p.Description, p.RetailPrice, p.WholesalePrice, p.WholesaleMoq, p.CompareAtPrice,
                                p.Rating, p.ReviewCount, p.Tone, p.Badge, p.StockStatus,
                                p.Sizes, p.Colors, p.ImageUrls, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);

        var bySlug = found.ToDictionary(p => p.Slug);
        // Re-order to match input order (WHERE slug IN (...) gives no ordering guarantee);
        // silently drops any slug that no longer resolves to a product.
        return slugs.Where(bySlug.ContainsKey).Select(s => bySlug[s]).ToList();
    }

    public async Task<List<ProductDto>> GetAutoFillAsync(ProductAutoFillRule rule, int limit, CancellationToken ct)
    {
        var query = from p in db.Products.AsNoTracking()
                    join c in db.Categories.AsNoTracking() on p.CategorySlug equals c.Slug into cj
                    from c in cj.DefaultIfEmpty()
                    where p.StockStatus != StockStatus.OutOfStock
                    select new { p, CategoryName = c != null ? c.Name : p.CategorySlug };

        query = rule switch
        {
            ProductAutoFillRule.Discounted => query
                .Where(x => x.p.CompareAtPrice != null && x.p.CompareAtPrice > x.p.RetailPrice)
                .OrderByDescending(x => x.p.CompareAtPrice! - x.p.RetailPrice),
            ProductAutoFillRule.BestSeller => query
                .Where(x => x.p.Badge == Badge.BestSeller)
                .OrderByDescending(x => x.p.CreatedAt),
            ProductAutoFillRule.WholesaleEligible => query
                .Where(x => x.p.WholesalePrice != null)
                .OrderByDescending(x => x.p.CreatedAt),
            _ => query.OrderByDescending(x => x.p.CreatedAt) // Newest
        };

        return await query.Take(limit)
            .Select(x => new ProductDto(x.p.Id, x.p.Slug, x.p.Name, x.p.CategorySlug, x.CategoryName,
                x.p.Description, x.p.RetailPrice, x.p.WholesalePrice, x.p.WholesaleMoq, x.p.CompareAtPrice,
                x.p.Rating, x.p.ReviewCount, x.p.Tone, x.p.Badge, x.p.StockStatus,
                x.p.Sizes, x.p.Colors, x.p.ImageUrls, x.p.CreatedAt, x.p.UpdatedAt))
            .ToListAsync(ct);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        db.Products.AnyAsync(p => p.Slug == slug, ct);

    public async Task AddAsync(Product product, CancellationToken ct)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Product product, CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Product product, CancellationToken ct)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
    }
}
