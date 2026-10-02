using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using DelightsBoutique.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICustomerFlagRepository> _customerFlags = new();
    private readonly Mock<IPhoneNormalizer> _phoneNormalizer = new();
    private readonly Mock<IOrderIdGenerator> _orderIdGenerator = new();
    private readonly Mock<IOrderPricingService> _pricing = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IOrderNotificationRepository> _notifications = new();

    private CreateOrderCommandHandler CreateHandler() => new(
        _products.Object, _customerFlags.Object, _phoneNormalizer.Object,
        _orderIdGenerator.Object, _pricing.Object, _orders.Object, _notifications.Object);

    private static CreateOrderCommand Command(params OrderLineRequest[] items) => new(
        "idem-key", [.. items], "Jane Doe", "0241234567",
        FulfillmentType.Pickup, null, PaymentMethod.CashOnDeliveryOrPickup, null);

    [Fact]
    public async Task Handle_HappyPath_ReturnsCorrectOrderDto()
    {
        _phoneNormalizer.Setup(p => p.Normalize("0241234567")).Returns("0241234567");
        _customerFlags.Setup(c => c.GetAsync("0241234567", It.IsAny<CancellationToken>())).ReturnsAsync((CustomerFlag?)null);

        var product = Product.Create("slug-1", "Product One", "cat", "desc", 50m, null, null, null, Tone.Rose, null, StockStatus.InStock);
        _products.Setup(p => p.GetEntityBySlugAsync("slug-1", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _pricing.Setup(p => p.ResolveUnitPrice(product, 2, false)).Returns(50m);
        _orderIdGenerator.Setup(g => g.NewId()).Returns("DB-TEST1");

        var result = await CreateHandler().Handle(Command(new OrderLineRequest("slug-1", 2, null, null)), CancellationToken.None);

        result.Id.Should().Be("DB-TEST1");
        result.Subtotal.Should().Be(100m);
        _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.AddAsync(It.IsAny<OrderNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnresolvableSlug_ThrowsValidationException()
    {
        _phoneNormalizer.Setup(p => p.Normalize(It.IsAny<string>())).Returns("0241234567");
        _customerFlags.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CustomerFlag?)null);
        _products.Setup(p => p.GetEntityBySlugAsync("missing-slug", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(Command(new OrderLineRequest("missing-slug", 1, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OutOfStockLine_ThrowsProductUnavailableException()
    {
        _phoneNormalizer.Setup(p => p.Normalize(It.IsAny<string>())).Returns("0241234567");
        _customerFlags.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CustomerFlag?)null);

        var product = Product.Create("slug-1", "Product One", "cat", "desc", 50m, null, null, null, Tone.Rose, null, StockStatus.OutOfStock);
        _products.Setup(p => p.GetEntityBySlugAsync("slug-1", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var act = () => CreateHandler().Handle(Command(new OrderLineRequest("slug-1", 1, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<ProductUnavailableException>();
        _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
