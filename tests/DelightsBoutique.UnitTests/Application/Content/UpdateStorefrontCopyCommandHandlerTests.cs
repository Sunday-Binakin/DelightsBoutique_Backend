using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Content.Commands;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Content;

public class UpdateStorefrontCopyCommandHandlerTests
{
    private readonly Mock<ISiteContentRepository> _siteContent = new();

    private UpdateStorefrontCopyCommandHandler CreateHandler() => new(_siteContent.Object);

    [Fact]
    public async Task Handle_ShallowMerge_OverwritesMatchingKeyAndPreservesUntouchedKey()
    {
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.StorefrontCopy, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"cartLabel":"Cart","checkoutLabel":"Checkout"}""");

        var patch = JsonDocument.Parse("""{"cartLabel":"My Cart"}""").RootElement;

        var result = await CreateHandler().Handle(new UpdateStorefrontCopyCommand(patch), CancellationToken.None);

        result.GetProperty("cartLabel").GetString().Should().Be("My Cart"); // overwritten
        result.GetProperty("checkoutLabel").GetString().Should().Be("Checkout"); // untouched, preserved
    }

    [Fact]
    public async Task Handle_PatchWithNewKey_InsertsIt()
    {
        _siteContent.Setup(r => r.GetValueJsonAsync(SiteContentKey.StorefrontCopy, It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"cartLabel":"Cart"}""");

        var patch = JsonDocument.Parse("""{"newsletterLabel":"Subscribe"}""").RootElement;

        var result = await CreateHandler().Handle(new UpdateStorefrontCopyCommand(patch), CancellationToken.None);

        result.GetProperty("cartLabel").GetString().Should().Be("Cart");
        result.GetProperty("newsletterLabel").GetString().Should().Be("Subscribe");
    }
}
