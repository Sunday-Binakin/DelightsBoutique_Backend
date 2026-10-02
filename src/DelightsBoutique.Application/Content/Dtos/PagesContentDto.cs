namespace DelightsBoutique.Application.Content.Dtos;

public record PageContentDto(string Title, string Body);

public record FaqItemDto(string Id, string Question, string Answer);

public record PagesContentDto(
    PageContentDto About, PageContentDto Shipping, PageContentDto Returns, PageContentDto Terms, PageContentDto Privacy,
    List<FaqItemDto> Faqs);
