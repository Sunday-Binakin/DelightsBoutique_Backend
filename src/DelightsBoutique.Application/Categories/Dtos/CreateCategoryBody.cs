using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Categories.Dtos;

public record CreateCategoryBody(string? Slug, string Name, Tone Tone, string? ImageUrl);
