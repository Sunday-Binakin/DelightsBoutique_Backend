using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Categories.Dtos;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfCategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<List<CategoryDto>> GetAllAsync(CancellationToken ct) =>
        db.Categories.AsNoTracking().Select(ToDto).ToListAsync(ct);

    public Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken ct) =>
        db.Categories.AsNoTracking().Where(c => c.Slug == slug).Select(ToDto).FirstOrDefaultAsync(ct);

    public Task<Category?> GetEntityBySlugAsync(string slug, CancellationToken ct) =>
        db.Categories.FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        db.Categories.AnyAsync(c => c.Slug == slug, ct);

    public Task<bool> IsReferencedByAnyProductAsync(string slug, CancellationToken ct) =>
        db.Products.AnyAsync(p => p.CategorySlug == slug, ct);

    public async Task AddAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Category category, CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Category category, CancellationToken ct)
    {
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Category, CategoryDto>> ToDto =
        c => new CategoryDto(c.Id, c.Slug, c.Name, c.Tone, c.ImageUrl, c.CreatedAt, c.UpdatedAt);
}
