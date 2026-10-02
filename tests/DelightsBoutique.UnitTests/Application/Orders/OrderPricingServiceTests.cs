using DelightsBoutique.Application.Orders.Services;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Application.Orders;

public class OrderPricingServiceTests
{
    private readonly OrderPricingService _pricing = new();

    [Fact]
    public void ResolveUnitPrice_RetailCustomer_ReturnsRetailPrice()
    {
        var product = Product.Create("slug", "Name", "cat", "desc", retailPrice: 100m,
            wholesalePrice: 80m, wholesaleMoq: 10, compareAtPrice: null, Tone.Rose, null, StockStatus.InStock);

        var price = _pricing.ResolveUnitPrice(product, quantity: 20, customerIsWholesale: false);

        price.Should().Be(100m);
    }

    [Fact]
    public void ResolveUnitPrice_WholesaleAtOrAboveMoq_ReturnsWholesalePrice()
    {
        var product = Product.Create("slug", "Name", "cat", "desc", retailPrice: 100m,
            wholesalePrice: 80m, wholesaleMoq: 10, compareAtPrice: null, Tone.Rose, null, StockStatus.InStock);

        var price = _pricing.ResolveUnitPrice(product, quantity: 10, customerIsWholesale: true);

        price.Should().Be(80m);
    }

    [Fact]
    public void ResolveUnitPrice_WholesaleBelowMoq_ReturnsRetailPrice()
    {
        var product = Product.Create("slug", "Name", "cat", "desc", retailPrice: 100m,
            wholesalePrice: 80m, wholesaleMoq: 10, compareAtPrice: null, Tone.Rose, null, StockStatus.InStock);

        var price = _pricing.ResolveUnitPrice(product, quantity: 5, customerIsWholesale: true);

        price.Should().Be(100m);
    }

    [Fact]
    public void ResolveUnitPrice_WholesaleButProductHasNoWholesalePricing_ReturnsRetailPrice()
    {
        var product = Product.Create("slug", "Name", "cat", "desc", retailPrice: 100m,
            wholesalePrice: null, wholesaleMoq: null, compareAtPrice: null, Tone.Rose, null, StockStatus.InStock);

        var price = _pricing.ResolveUnitPrice(product, quantity: 100, customerIsWholesale: true);

        price.Should().Be(100m);
    }
}
