using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Categories.Dtos;

public record CategoryDto(Guid Id, string Slug, string Name, Tone Tone, string? ImageUrl, DateTime CreatedAt, DateTime UpdatedAt);
