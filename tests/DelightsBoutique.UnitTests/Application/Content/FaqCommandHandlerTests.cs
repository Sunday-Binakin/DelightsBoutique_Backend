using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Commands;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Content;

public class FaqCommandHandlerTests
{
    private readonly Mock<ISiteContentRepository> _siteContent = new();

    private static PagesContentDto PagesWithFaqs(params FaqItemDto[] faqs) => new(
        new PageContentDto("About", "body"), new PageContentDto("Shipping", "body"),
        new PageContentDto("Returns", "body"), new PageContentDto("Terms", "body"), new PageContentDto("Privacy", "body"),
        [.. faqs]);

    [Fact]
    public async Task AddFaq_AppendsNewFaqWithGeneratedId()
    {
        var pages = PagesWithFaqs();
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.PagesContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(pages, ContentJsonOptions.Default));

        var handler = new AddFaqCommandHandler(_siteContent.Object);
        var result = await handler.Handle(new AddFaqCommand("Q?", "A."), CancellationToken.None);

        result.Id.Should().NotBeNullOrEmpty();
        result.Question.Should().Be("Q?");
        result.Answer.Should().Be("A.");
        _siteContent.Verify(r => r.UpsertAsync(SiteContentKey.PagesContent,
            It.Is<string>(json => json.Contains("Q?")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFaq_UnknownId_ReturnsNull()
    {
        var pages = PagesWithFaqs(new FaqItemDto("id1", "Q1", "A1"));
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.PagesContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(pages, ContentJsonOptions.Default));

        var handler = new UpdateFaqCommandHandler(_siteContent.Object);
        var result = await handler.Handle(new UpdateFaqCommand("missing-id", "New Q", null), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateFaq_KnownId_MergesFieldsAndPreservesUnspecified()
    {
        var pages = PagesWithFaqs(new FaqItemDto("id1", "Original Q", "Original A"));
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.PagesContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(pages, ContentJsonOptions.Default));

        var handler = new UpdateFaqCommandHandler(_siteContent.Object);
        var result = await handler.Handle(new UpdateFaqCommand("id1", "New Q", null), CancellationToken.None);

        result!.Question.Should().Be("New Q");
        result.Answer.Should().Be("Original A"); // untouched
    }

    [Fact]
    public async Task DeleteFaq_UnknownId_ReturnsFalse()
    {
        var pages = PagesWithFaqs(new FaqItemDto("id1", "Q1", "A1"));
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.PagesContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(pages, ContentJsonOptions.Default));

        var handler = new DeleteFaqCommandHandler(_siteContent.Object);
        var result = await handler.Handle(new DeleteFaqCommand("missing-id"), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFaq_KnownId_RemovesItAndReturnsTrue()
    {
        var pages = PagesWithFaqs(new FaqItemDto("id1", "Q1", "A1"), new FaqItemDto("id2", "Q2", "A2"));
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.PagesContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(pages, ContentJsonOptions.Default));

        var handler = new DeleteFaqCommandHandler(_siteContent.Object);
        var result = await handler.Handle(new DeleteFaqCommand("id1"), CancellationToken.None);

        result.Should().BeTrue();
        _siteContent.Verify(r => r.UpsertAsync(SiteContentKey.PagesContent,
            It.Is<string>(json => !json.Contains("\"id1\"") && json.Contains("\"id2\"")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
