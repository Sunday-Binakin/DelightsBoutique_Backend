using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;

namespace DelightsBoutique.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Tone Tone { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Category() { }

    /// <summary>Test-support only.</summary>
    internal static Category ForTesting(string slug) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = "Test Category",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static Category Create(string slug, string name, Tone tone, string? imageUrl = null)
    {
        var now = DateTime.UtcNow;
        return new Category
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            Tone = tone,
            ImageUrl = imageUrl,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// "new value, or null = leave unchanged" — including <see cref="ImageUrl"/>, which is
    /// itself nullable. A PATCH cannot explicitly clear an already-set ImageUrl back to null
    /// this phase — an accepted limitation, not an oversight.
    /// </summary>
    public void Update(string? name = null, Tone? tone = null, string? imageUrl = null)
    {
        Name = name ?? Name;
        Tone = tone ?? Tone;
        ImageUrl = imageUrl ?? ImageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The "is referenced" check is a DB query, so the caller (an Application handler)
    /// resolves it and passes the result in — Domain stays free of persistence concerns.
    /// </summary>
    public void EnsureDeletable(bool isReferencedByAnyProduct)
    {
        if (isReferencedByAnyProduct)
            throw new CategoryInUseException(Slug);
    }
}
