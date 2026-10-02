using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Products.Dtos;

public record ProductSearchFilter(string? CategorySlug, string? Search, Badge? Badge, string? Sort, int Page, int Limit);
