using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;

namespace DelightsBoutique.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string CategorySlug { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal RetailPrice { get; private set; }
    public decimal? WholesalePrice { get; private set; }
    public int? WholesaleMoq { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public decimal Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public Tone Tone { get; private set; }
    public Badge? Badge { get; private set; }
    public StockStatus StockStatus { get; private set; }
    public List<string> Sizes { get; private set; } = [];
    public List<string> Colors { get; private set; } = [];
    public List<string> ImageUrls { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Product() { }

    /// <summary>Test-support only, until Phase 2's real Product creation handler exists.</summary>
    internal static Product ForTesting(string slug, StockStatus stockStatus) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = "Test Product",
        CategorySlug = "test-category",
        Description = string.Empty,
        StockStatus = stockStatus,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void EnsureAvailable()
    {
        if (StockStatus == StockStatus.OutOfStock)
            throw new ProductUnavailableException(Slug);
    }
}
