using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Entities;
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

    public async Task<PagedResult<ProductDto>> SearchAsync(ProductSearchFilter filter, CancellationToken ct)
    {
        // Anonymous-type projection, not a named ValueTuple — EF Core's SQL translator does
        // not reliably handle a named tuple's field access when the query is subsequently
        // composed with Where/OrderBy across multiple statements (throws at runtime:
        // "could not be translated"). Anonymous types are the well-supported idiom here.
        var query =
            from p in db.Products.AsNoTracking()
            join c in db.Categories.AsNoTracking() on p.CategorySlug equals c.Slug into cj
            from c in cj.DefaultIfEmpty()
            select new { p, CategoryName = c != null ? c.Name : p.CategorySlug };

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            query = query.Where(x => x.p.CategorySlug == filter.CategorySlug);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(x =>
                EF.Functions.ILike(x.p.Name, $"%{filter.Search}%") ||
                EF.Functions.ILike(x.p.Description, $"%{filter.Search}%"));

        if (filter.Badge is not null)
            query = query.Where(x => x.p.Badge == filter.Badge);

        query = filter.Sort switch
        {
            "price-asc" => query.OrderBy(x => x.p.RetailPrice).ThenBy(x => x.p.Id),
            "price-desc" => query.OrderByDescending(x => x.p.RetailPrice).ThenBy(x => x.p.Id),
            "rating" => query.OrderByDescending(x => x.p.Rating).ThenBy(x => x.p.Id),
            _ => query.OrderByDescending(x => x.p.CreatedAt).ThenBy(x => x.p.Id) // "newest" default; ThenBy stabilizes paging across ties
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
