using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Content.Dtos;

public record BannerDto(string Eyebrow, string Title, string CtaLabel, string Href, Tone Tone,
    string ArtCategorySlug, string? ImageUrl, string? TextColor);

public record BannerSetDto(BannerDto HeroMain, BannerDto HeroSide1, BannerDto HeroSide2, BannerDto FullWidth);
