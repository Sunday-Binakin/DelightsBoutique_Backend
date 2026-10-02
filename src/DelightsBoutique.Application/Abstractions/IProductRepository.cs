using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IProductRepository
{
    Task<ProductDto?> GetBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Tracked entity, for admin mutation.</summary>
    Task<Product?> GetEntityBySlugAsync(string slug, CancellationToken ct);

    Task<PagedResult<ProductDto>> SearchAsync(ProductSearchFilter filter, CancellationToken ct);

    /// <summary>Preserves the input slug order (admin-curated display order) — SQL's
    /// WHERE slug IN (...) does not guarantee this. Silently drops slugs that no longer
    /// resolve to a product.</summary>
    Task<List<ProductDto>> GetBySlugsAsync(IReadOnlyList<string> slugs, CancellationToken ct);

    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);

    Task AddAsync(Product product, CancellationToken ct);
    Task UpdateAsync(Product product, CancellationToken ct);
    Task DeleteAsync(Product product, CancellationToken ct);
}
