using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Commands;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Products;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();

    private CreateProductCommandHandler CreateHandler() => new(_products.Object);

    private static CreateProductCommand Command(string? slug = null) => new(
        slug, "Rose Scarf", "accessories", "A lovely scarf",
        50m, null, null, null, null, null, Tone.Rose, null, StockStatus.InStock, null, null, null);

    [Fact]
    public async Task Handle_SlugProvided_UsesProvidedSlug()
    {
        _products.Setup(r => r.SlugExistsAsync("my-slug", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _products.Setup(r => r.GetBySlugAsync("my-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(Guid.NewGuid(), "my-slug", "Rose Scarf", "accessories", "Accessories",
                "desc", 50m, null, null, null, 0, 0, Tone.Rose, null, StockStatus.InStock, [], [], [], DateTime.UtcNow, DateTime.UtcNow));

        var result = await CreateHandler().Handle(Command("my-slug"), CancellationToken.None);

        result.Slug.Should().Be("my-slug");
        _products.Verify(r => r.AddAsync(It.Is<Product>(p => p.Slug == "my-slug"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SlugOmitted_GeneratesFromName()
    {
        _products.Setup(r => r.SlugExistsAsync("rose-scarf", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _products.Setup(r => r.GetBySlugAsync("rose-scarf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(Guid.NewGuid(), "rose-scarf", "Rose Scarf", "accessories", "Accessories",
                "desc", 50m, null, null, null, 0, 0, Tone.Rose, null, StockStatus.InStock, [], [], [], DateTime.UtcNow, DateTime.UtcNow));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Slug.Should().Be("rose-scarf");
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsDuplicateSlugException()
    {
        _products.Setup(r => r.SlugExistsAsync("my-slug", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(Command("my-slug"), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSlugException>();
        _products.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
