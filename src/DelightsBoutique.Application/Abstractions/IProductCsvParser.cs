using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Abstractions;

/// <summary>One parsed CSV row. ParseError is set only when a cell's raw text can't become
/// a typed value at all (bad decimal syntax, unrecognized enum name) — anything that's a
/// normal business-rule failure (empty name, nonexistent category, price &lt;= 0) is left to
/// flow through as a real value and gets caught by the existing CreateProductCommandValidator,
/// so there is exactly one place that owns "what makes a product valid."</summary>
public record ProductCsvRow(
    int RowNumber, string? Slug, string Name, string CategorySlug, string Description,
    decimal RetailPrice, decimal? WholesalePrice, int? WholesaleMoq, decimal? CompareAtPrice,
    Tone Tone, Badge? Badge, StockStatus StockStatus,
    List<string>? Sizes, List<string>? Colors, List<string>? ImageUrls, string? ParseError);

public interface IProductCsvParser
{
    List<ProductCsvRow> Parse(byte[] fileBytes);
}
