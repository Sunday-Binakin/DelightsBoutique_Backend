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

    /// <summary>Test-support only.</summary>
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

    public static Product Create(
        string slug, string name, string categorySlug, string description,
        decimal retailPrice, decimal? wholesalePrice, int? wholesaleMoq, decimal? compareAtPrice,
        Tone tone, Badge? badge, StockStatus stockStatus,
        IReadOnlyList<string>? sizes = null, IReadOnlyList<string>? colors = null, IReadOnlyList<string>? imageUrls = null,
        decimal rating = 0, int reviewCount = 0)
    {
        var now = DateTime.UtcNow;
        return new Product
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            CategorySlug = categorySlug,
            Description = description,
            RetailPrice = retailPrice,
            WholesalePrice = wholesalePrice,
            WholesaleMoq = wholesaleMoq,
            CompareAtPrice = compareAtPrice,
            Rating = rating,
            ReviewCount = reviewCount,
            Tone = tone,
            Badge = badge,
            StockStatus = stockStatus,
            Sizes = sizes?.ToList() ?? [],
            Colors = colors?.ToList() ?? [],
            ImageUrls = imageUrls?.ToList() ?? [],
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Every parameter is "new value, or null = leave unchanged" — including fields that
    /// are themselves nullable (<see cref="WholesalePrice"/>, <see cref="CompareAtPrice"/>,
    /// <see cref="Badge"/>). This means a PATCH cannot explicitly clear one of those back to
    /// null once set — an accepted Phase 2 limitation, not an oversight.
    /// </summary>
    public void Update(
        string? name = null, string? categorySlug = null, string? description = null,
        decimal? retailPrice = null, decimal? wholesalePrice = null, int? wholesaleMoq = null, decimal? compareAtPrice = null,
        decimal? rating = null, int? reviewCount = null, Tone? tone = null, Badge? badge = null, StockStatus? stockStatus = null,
        IReadOnlyList<string>? sizes = null, IReadOnlyList<string>? colors = null, IReadOnlyList<string>? imageUrls = null)
    {
        Name = name ?? Name;
        CategorySlug = categorySlug ?? CategorySlug;
        Description = description ?? Description;
        RetailPrice = retailPrice ?? RetailPrice;
        WholesalePrice = wholesalePrice ?? WholesalePrice;
        WholesaleMoq = wholesaleMoq ?? WholesaleMoq;
        CompareAtPrice = compareAtPrice ?? CompareAtPrice;
        Rating = rating ?? Rating;
        ReviewCount = reviewCount ?? ReviewCount;
        Tone = tone ?? Tone;
        Badge = badge ?? Badge;
        StockStatus = stockStatus ?? StockStatus;
        Sizes = sizes?.ToList() ?? Sizes;
        Colors = colors?.ToList() ?? Colors;
        ImageUrls = imageUrls?.ToList() ?? ImageUrls;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnsureAvailable()
    {
        if (StockStatus == StockStatus.OutOfStock)
            throw new ProductUnavailableException(Slug);
    }
}
