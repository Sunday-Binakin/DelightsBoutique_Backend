using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using DelightsBoutique.Domain.ValueObjects;
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

    [Fact]
    public void Create_SingleLine_SetsSubtotalAndPendingConfirmationStatus()
    {
        var lines = new[] { new OrderLineInput("slug-1", "Product One", 2, 25m, "M", "Red") };

        var order = Order.Create("DB-TEST1", lines, "Jane Doe", "0241234567",
            FulfillmentType.Pickup, null, PaymentMethod.CashOnDeliveryOrPickup, "Please call ahead");

        order.Status.Should().Be(OrderStatus.PendingConfirmation);
        order.Subtotal.Should().Be(50m);
        order.Items.Should().HaveCount(1);
        order.Items[0].Slug.Should().Be("slug-1");
        order.Items[0].OrderId.Should().Be("DB-TEST1");
    }

    [Fact]
    public void Create_MultipleLines_SumsSubtotalAcrossLines()
    {
        var lines = new[]
        {
            new OrderLineInput("slug-1", "Product One", 2, 25m, null, null),
            new OrderLineInput("slug-2", "Product Two", 1, 100m, null, null)
        };

        var order = Order.Create("DB-TEST2", lines, "Jane Doe", "0241234567",
            FulfillmentType.Delivery, "123 Main St", PaymentMethod.MobileMoney, null);

        order.Subtotal.Should().Be(150m); // (2*25) + (1*100)
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Create_EmptyLines_ThrowsArgumentException()
    {
        var act = () => Order.Create("DB-TEST3", [], "Jane Doe", "0241234567",
            FulfillmentType.Pickup, null, PaymentMethod.CashOnDeliveryOrPickup, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPaymentProofUrl_WhenPendingConfirmation_SetsUrl()
    {
        var order = Order.ForTesting(OrderStatus.PendingConfirmation, FulfillmentType.Pickup);

        order.SetPaymentProofUrl("https://example.com/proof.jpg");

        order.PaymentProofUrl.Should().Be("https://example.com/proof.jpg");
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.OutForDelivery)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void SetPaymentProofUrl_WhenNotPendingConfirmation_ThrowsPaymentProofNotAllowedException(OrderStatus status)
    {
        var order = Order.ForTesting(status, FulfillmentType.Delivery);

        var act = () => order.SetPaymentProofUrl("https://example.com/proof.jpg");

        act.Should().Throw<PaymentProofNotAllowedException>();
    }

    [Fact]
    public void SetInternalNotes_SetsNotesRegardlessOfStatus()
    {
        var order = Order.ForTesting(OrderStatus.Completed, FulfillmentType.Delivery);

        order.SetInternalNotes("Called customer to confirm address");

        order.InternalNotes.Should().Be("Called customer to confirm address");
    }

    [Fact]
    public void SetInternalNotes_Null_ClearsNotes()
    {
        var order = Order.ForTesting(OrderStatus.PendingConfirmation, FulfillmentType.Pickup);
        order.SetInternalNotes("some note");

        order.SetInternalNotes(null);

        order.InternalNotes.Should().BeNull();
    }
}
