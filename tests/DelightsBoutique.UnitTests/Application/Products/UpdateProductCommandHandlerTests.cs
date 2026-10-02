using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Commands;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Products;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();

    private UpdateProductCommandHandler CreateHandler() => new(_products.Object);

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNull()
    {
        _products.Setup(r => r.GetEntityBySlugAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await CreateHandler().Handle(
            new UpdateProductCommand("missing", "New Name", null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        result.Should().BeNull();
        _products.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductFound_UpdatesAndReturnsDto()
    {
        var product = Product.Create("slug", "Original", "cat", "desc", 10m, null, null, null, Tone.Cream, null, StockStatus.InStock);
        _products.Setup(r => r.GetEntityBySlugAsync("slug", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _products.Setup(r => r.GetBySlugAsync("slug", It.IsAny<CancellationToken>())).ReturnsAsync((DelightsBoutique.Application.Products.Dtos.ProductDto?)null);

        await CreateHandler().Handle(
            new UpdateProductCommand("slug", "New Name", null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        product.Name.Should().Be("New Name");
        _products.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }
}
