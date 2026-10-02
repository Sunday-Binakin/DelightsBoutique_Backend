using DelightsBoutique.Application.Categories.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface ICategoryRepository
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken ct);
    Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Tracked entity, for admin mutation.</summary>
    Task<Category?> GetEntityBySlugAsync(string slug, CancellationToken ct);

    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);

    /// <summary>Queries Products directly — same DbContext, no generic Repository&lt;T&gt; needed.</summary>
    Task<bool> IsReferencedByAnyProductAsync(string slug, CancellationToken ct);

    Task AddAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task DeleteAsync(Category category, CancellationToken ct);
}
