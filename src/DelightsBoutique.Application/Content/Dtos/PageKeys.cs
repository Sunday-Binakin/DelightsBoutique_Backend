namespace DelightsBoutique.Application.Content.Dtos;

public static class PageKeys
{
    public static PageContentDto? Select(PagesContentDto pages, string key) => key.ToLowerInvariant() switch
    {
        "about" => pages.About,
        "shipping" => pages.Shipping,
        "returns" => pages.Returns,
        "terms" => pages.Terms,
        "privacy" => pages.Privacy,
        _ => null
    };

    public static PagesContentDto? With(PagesContentDto pages, string key, PageContentDto page) => key.ToLowerInvariant() switch
    {
        "about" => pages with { About = page },
        "shipping" => pages with { Shipping = page },
        "returns" => pages with { Returns = page },
        "terms" => pages with { Terms = page },
        "privacy" => pages with { Privacy = page },
        _ => null
    };
}
