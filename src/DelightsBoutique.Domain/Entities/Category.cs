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

    /// <summary>Test-support only, until Phase 2's real Category creation handler exists.</summary>
    internal static Category ForTesting(string slug) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = "Test Category",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

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
