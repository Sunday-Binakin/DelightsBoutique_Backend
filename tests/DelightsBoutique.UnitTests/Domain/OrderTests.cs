using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class OrderTests
{
    [Theory]
    [InlineData(OrderStatus.PendingConfirmation, OrderStatus.Confirmed, FulfillmentType.Delivery)]
    [InlineData(OrderStatus.PendingConfirmation, OrderStatus.Confirmed, FulfillmentType.Pickup)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing, FulfillmentType.Delivery)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing, FulfillmentType.Pickup)]
    [InlineData(OrderStatus.Processing, OrderStatus.OutForDelivery, FulfillmentType.Delivery)]
    [InlineData(OrderStatus.Processing, OrderStatus.ReadyForPickup, FulfillmentType.Pickup)]
    [InlineData(OrderStatus.OutForDelivery, OrderStatus.Completed, FulfillmentType.Delivery)]
    [InlineData(OrderStatus.ReadyForPickup, OrderStatus.Completed, FulfillmentType.Pickup)]
    public void TransitionTo_LegalTransition_Succeeds(OrderStatus from, OrderStatus to, FulfillmentType fulfillment)
    {
        var order = Order.ForTesting(from, fulfillment);

        order.TransitionTo(to);

        order.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(OrderStatus.Processing, OrderStatus.OutForDelivery, FulfillmentType.Pickup)] // wrong fulfillment branch
    [InlineData(OrderStatus.Processing, OrderStatus.ReadyForPickup, FulfillmentType.Delivery)] // wrong fulfillment branch
    [InlineData(OrderStatus.PendingConfirmation, OrderStatus.Processing, FulfillmentType.Delivery)] // skips Confirmed
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed, FulfillmentType.Delivery)] // skips Processing/OutForDelivery
    [InlineData(OrderStatus.Completed, OrderStatus.Processing, FulfillmentType.Delivery)] // terminal state
    public void TransitionTo_IllegalTransition_Throws(OrderStatus from, OrderStatus to, FulfillmentType fulfillment)
    {
        var order = Order.ForTesting(from, fulfillment);

        var act = () => order.TransitionTo(to);

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Theory]
    [InlineData(OrderStatus.PendingConfirmation)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    public void TransitionTo_Cancelled_FromPendingConfirmationConfirmedOrProcessing_Succeeds(OrderStatus from)
    {
        var order = Order.ForTesting(from, FulfillmentType.Delivery);

        order.TransitionTo(OrderStatus.Cancelled);

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(OrderStatus.OutForDelivery)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.Completed)]
    public void TransitionTo_Cancelled_FromOutForDeliveryReadyForPickupOrCompleted_Throws(OrderStatus from)
    {
        var order = Order.ForTesting(from, FulfillmentType.Delivery);

        var act = () => order.TransitionTo(OrderStatus.Cancelled);

        act.Should().Throw<InvalidStatusTransitionException>();
    }
}
