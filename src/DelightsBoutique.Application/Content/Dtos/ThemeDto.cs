namespace DelightsBoutique.Application.Content.Dtos;

/// <summary>
/// The owner picks these two colors only — every other shade (hover/dark
/// variants, etc.) is derived from them on the frontend, the same way the
/// storefront's current flyer-matched palette was built by hand.
/// </summary>
public record ThemeDto(string Primary, string Accent);
