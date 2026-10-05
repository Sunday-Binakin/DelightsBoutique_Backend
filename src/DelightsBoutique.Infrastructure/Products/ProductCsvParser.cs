using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Infrastructure.Products;

/// <summary>CsvHelper is a third-party parsing library, so it stays behind this
/// Infrastructure implementation of an Application-defined interface — same pattern as
/// ImageSharpImageValidator behind IImageValidator. Confirmed column schema (per the
/// frontend's R3 integration request): slug,name,categorySlug,description,retailPrice,
/// wholesalePrice,wholesaleMoq,compareAtPrice,tone,badge,stockStatus,sizes,colors,
/// imageUrls — sizes/colors/imageUrls are ';'-separated within their cell to avoid
/// colliding with CSV's own ',' delimiter. A row whose slug already exists on another
/// product fails that row (DuplicateSlugException from the reused CreateProductCommand
/// pipeline) rather than updating it — bulk upload never silently overwrites existing
/// catalog data.</summary>
public class ProductCsvParser : IProductCsvParser
{
    private class RawRow
    {
        [Name("slug")] public string? Slug { get; set; }
        [Name("name")] public string? Name { get; set; }
        [Name("categorySlug")] public string? CategorySlug { get; set; }
        [Name("description")] public string? Description { get; set; }
        [Name("retailPrice")] public string? RetailPrice { get; set; }
        [Name("wholesalePrice")] public string? WholesalePrice { get; set; }
        [Name("wholesaleMoq")] public string? WholesaleMoq { get; set; }
        [Name("compareAtPrice")] public string? CompareAtPrice { get; set; }
        [Name("tone")] public string? Tone { get; set; }
        [Name("badge")] public string? Badge { get; set; }
        [Name("stockStatus")] public string? StockStatus { get; set; }
        [Name("sizes")] public string? Sizes { get; set; }
        [Name("colors")] public string? Colors { get; set; }
        [Name("imageUrls")] public string? ImageUrls { get; set; }
    }

    public List<ProductCsvRow> Parse(byte[] fileBytes)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var stream = new MemoryStream(fileBytes);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        var rows = new List<ProductCsvRow>();
        var rowNumber = 0;
        foreach (var raw in csv.GetRecords<RawRow>())
        {
            rowNumber++;
            rows.Add(ParseRow(rowNumber, raw));
        }

        return rows;
    }

    private static ProductCsvRow ParseRow(int rowNumber, RawRow raw)
    {
        string? error = null;

        if (!decimal.TryParse(raw.RetailPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var retailPrice))
            error = Append(error, $"retailPrice '{raw.RetailPrice}' is not a valid number.");

        decimal? wholesalePrice = null;
        if (!string.IsNullOrWhiteSpace(raw.WholesalePrice))
        {
            if (decimal.TryParse(raw.WholesalePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var wp)) wholesalePrice = wp;
            else error = Append(error, $"wholesalePrice '{raw.WholesalePrice}' is not a valid number.");
        }

        int? wholesaleMoq = null;
        if (!string.IsNullOrWhiteSpace(raw.WholesaleMoq))
        {
            if (int.TryParse(raw.WholesaleMoq, out var moq)) wholesaleMoq = moq;
            else error = Append(error, $"wholesaleMoq '{raw.WholesaleMoq}' is not a valid integer.");
        }

        decimal? compareAtPrice = null;
        if (!string.IsNullOrWhiteSpace(raw.CompareAtPrice))
        {
            if (decimal.TryParse(raw.CompareAtPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var cap)) compareAtPrice = cap;
            else error = Append(error, $"compareAtPrice '{raw.CompareAtPrice}' is not a valid number.");
        }

        if (!Enum.TryParse<Tone>(raw.Tone, ignoreCase: true, out var tone))
            error = Append(error, $"tone '{raw.Tone}' is not one of: {string.Join(", ", Enum.GetNames<Tone>())}.");

        Badge? badge = null;
        if (!string.IsNullOrWhiteSpace(raw.Badge))
        {
            if (Enum.TryParse<Badge>(raw.Badge, ignoreCase: true, out var b)) badge = b;
            else error = Append(error, $"badge '{raw.Badge}' is not one of: {string.Join(", ", Enum.GetNames<Badge>())}.");
        }

        if (!Enum.TryParse<StockStatus>(raw.StockStatus, ignoreCase: true, out var stockStatus))
            error = Append(error, $"stockStatus '{raw.StockStatus}' is not one of: {string.Join(", ", Enum.GetNames<StockStatus>())}.");

        return new ProductCsvRow(
            rowNumber, raw.Slug, raw.Name ?? "", raw.CategorySlug ?? "", raw.Description ?? "",
            retailPrice, wholesalePrice, wholesaleMoq, compareAtPrice,
            tone, badge, stockStatus,
            SplitMultiValue(raw.Sizes), SplitMultiValue(raw.Colors), SplitMultiValue(raw.ImageUrls), error);
    }

    private static List<string>? SplitMultiValue(string? cell) =>
        string.IsNullOrWhiteSpace(cell)
            ? null
            : cell.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string Append(string? existing, string next) => existing is null ? next : $"{existing} {next}";
}
