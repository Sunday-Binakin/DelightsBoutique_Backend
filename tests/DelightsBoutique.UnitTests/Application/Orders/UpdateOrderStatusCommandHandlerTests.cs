using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Orders;

public class UpdateOrderStatusCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();

    private UpdateOrderStatusCommandHandler CreateHandler() => new(_orders.Object);

    [Fact]
    public async Task Handle_LegalStatusTransition_UpdatesStatusAndSaves()
    {
        var order = Order.ForTesting(OrderStatus.PendingConfirmation, FulfillmentType.Pickup);
        _orders.Setup(o => o.GetEntityByIdAsync("DB-TEST", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand("DB-TEST", OrderStatus.Confirmed, null), CancellationToken.None);

        result!.Status.Should().Be(OrderStatus.Confirmed);
        _orders.Verify(o => o.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IllegalStatusTransition_ThrowsInvalidStatusTransitionException()
    {
        var order = Order.ForTesting(OrderStatus.PendingConfirmation, FulfillmentType.Pickup);
        _orders.Setup(o => o.GetEntityByIdAsync("DB-TEST", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var act = () => CreateHandler().Handle(new UpdateOrderStatusCommand("DB-TEST", OrderStatus.Completed, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStatusTransitionException>();
        _orders.Verify(o => o.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotesOnlyUpdate_DoesNotChangeStatus()
    {
        var order = Order.ForTesting(OrderStatus.Confirmed, FulfillmentType.Pickup);
        _orders.Setup(o => o.GetEntityByIdAsync("DB-TEST", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand("DB-TEST", null, "Called customer"), CancellationToken.None);

        result!.Status.Should().Be(OrderStatus.Confirmed);
        result.InternalNotes.Should().Be("Called customer");
    }

    [Fact]
    public async Task Handle_UnknownOrderId_ReturnsNull()
    {
        _orders.Setup(o => o.GetEntityByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await CreateHandler().Handle(new UpdateOrderStatusCommand("missing", OrderStatus.Confirmed, null), CancellationToken.None);

        result.Should().BeNull();
    }
}
