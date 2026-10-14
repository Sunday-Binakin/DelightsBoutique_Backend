using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Products.Dtos;

public record ProductSearchFilter(
    string? CategorySlug, string? Search, Badge? Badge, string? Sort, int Page, int Limit,
    decimal? MinPrice = null, decimal? MaxPrice = null, List<string>? Sizes = null, List<string>? Colors = null);

/// <summary>Same scope filters as ProductSearchFilter minus Sort/Page/Limit (facets
/// aren't paginated or sorted) and minus Sizes/Colors (those are what this is reporting
/// options for — a facet must stay unnarrowed by its own dimension).</summary>
public record ProductFacetsFilter(string? CategorySlug, string? Search, Badge? Badge, decimal? MinPrice = null, decimal? MaxPrice = null);

public record ProductFacetsDto(List<string> Sizes, List<string> Colors);
