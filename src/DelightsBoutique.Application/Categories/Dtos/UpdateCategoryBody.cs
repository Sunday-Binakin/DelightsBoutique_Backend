using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Categories.Dtos;

public record UpdateCategoryBody(string? Name, Tone? Tone, string? ImageUrl);
