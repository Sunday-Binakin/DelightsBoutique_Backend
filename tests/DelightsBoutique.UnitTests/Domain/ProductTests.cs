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

    [Fact]
    public void Create_SetsAllFields()
    {
        var product = Product.Create(
            "rose-scarf", "Rose Scarf", "accessories", "A lovely scarf",
            retailPrice: 50m, wholesalePrice: 40m, wholesaleMoq: 10, compareAtPrice: 60m,
            tone: Tone.Rose, badge: Badge.New, stockStatus: StockStatus.InStock,
            sizes: ["S", "M"], colors: ["Red"], imageUrls: ["https://example.com/1.jpg"]);

        product.Slug.Should().Be("rose-scarf");
        product.Name.Should().Be("Rose Scarf");
        product.CategorySlug.Should().Be("accessories");
        product.RetailPrice.Should().Be(50m);
        product.WholesalePrice.Should().Be(40m);
        product.WholesaleMoq.Should().Be(10);
        product.CompareAtPrice.Should().Be(60m);
        product.Tone.Should().Be(Tone.Rose);
        product.Badge.Should().Be(Badge.New);
        product.StockStatus.Should().Be(StockStatus.InStock);
        product.Sizes.Should().Equal("S", "M");
        product.Colors.Should().Equal("Red");
        product.ImageUrls.Should().Equal("https://example.com/1.jpg");
        product.CreatedAt.Should().Be(product.UpdatedAt);
    }

    [Fact]
    public void Update_WithNonNullFields_OverwritesOnlyThoseFields()
    {
        var product = Product.Create("slug", "Original Name", "cat", "desc", 10m, null, null, null, Tone.Cream, null, StockStatus.InStock);

        product.Update(name: "New Name", retailPrice: 20m);

        product.Name.Should().Be("New Name");
        product.RetailPrice.Should().Be(20m);
        product.CategorySlug.Should().Be("cat"); // untouched field preserved
        product.Description.Should().Be("desc");
    }

    [Fact]
    public void Update_WithNullFields_LeavesExistingValuesUnchanged()
    {
        var product = Product.Create("slug", "Name", "cat", "desc", 10m, wholesalePrice: 8m, wholesaleMoq: 5, compareAtPrice: 15m,
            tone: Tone.Gold, badge: Badge.BestSeller, stockStatus: StockStatus.LowStock);

        // Calling Update with everything omitted/null must be a true no-op on every field —
        // this is the test that proves the documented PATCH limitation (can't clear a
        // nullable field back to null) rather than just asserting it in a comment.
        product.Update();

        product.WholesalePrice.Should().Be(8m);
        product.WholesaleMoq.Should().Be(5);
        product.CompareAtPrice.Should().Be(15m);
        product.Badge.Should().Be(Badge.BestSeller);
        product.Tone.Should().Be(Tone.Gold);
        product.StockStatus.Should().Be(StockStatus.LowStock);
    }

    [Fact]
    public void Update_AlwaysBumpsUpdatedAt()
    {
        var product = Product.Create("slug", "Name", "cat", "desc", 10m, null, null, null, Tone.Cream, null, StockStatus.InStock);
        var originalUpdatedAt = product.UpdatedAt;

        Thread.Sleep(5);
        product.Update(name: "New Name");

        product.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
