using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void EnsureAvailable_WhenOutOfStock_ThrowsProductUnavailableException()
    {
        var product = Product.ForTesting("test-slug", StockStatus.OutOfStock);

        var act = product.EnsureAvailable;

        act.Should().Throw<ProductUnavailableException>()
            .Which.Slug.Should().Be("test-slug");
    }

    [Theory]
    [InlineData(StockStatus.InStock)]
    [InlineData(StockStatus.LowStock)]
    public void EnsureAvailable_WhenInStockOrLowStock_DoesNotThrow(StockStatus stockStatus)
    {
        var product = Product.ForTesting("test-slug", stockStatus);

        var act = product.EnsureAvailable;

        act.Should().NotThrow();
    }
}
