using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using DelightsBoutique.Domain.ValueObjects;

namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// Aggregate root. Owns its <see cref="OrderItem"/> collection — items are never
/// fetched or modified independent of their order.
/// </summary>
public class Order
{
    public string Id { get; private set; } = default!; // e.g. "DB-7Q3K9X2A" — opaque, not sequential
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public List<OrderItem> Items { get; private set; } = [];
    public decimal Subtotal { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public FulfillmentType Fulfillment { get; private set; }
    public string? Address { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? PaymentProofUrl { get; private set; }
    public string? Notes { get; private set; }
    public string? InternalNotes { get; private set; }

    protected Order() { }

    public static Order Create(
        string id, IReadOnlyList<OrderLineInput> lines, string customerName, string normalizedPhone,
        FulfillmentType fulfillment, string? address, PaymentMethod paymentMethod, string? notes)
    {
        if (lines.Count == 0)
            throw new ArgumentException("An order must have at least one line.", nameof(lines)); // defensive last resort — the real 400 comes from CreateOrderCommandValidator

        var order = new Order
        {
            Id = id,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.PendingConfirmation,
            CustomerName = customerName,
            Phone = normalizedPhone, // caller passes the already-normalized phone — Order never normalizes
            Fulfillment = fulfillment,
            Address = address,
            PaymentMethod = paymentMethod,
            Notes = notes,
            Subtotal = lines.Sum(l => l.Quantity * l.UnitPrice)
        };

        order.Items = lines.Select(l => OrderItem.Create(order.Id, l.Slug, l.Name, l.Quantity, l.UnitPrice, l.Size, l.Color)).ToList();
        return order;
    }

    public void SetPaymentProofUrl(string url)
    {
        if (Status != OrderStatus.PendingConfirmation)
            throw new PaymentProofNotAllowedException(Id, Status);

        PaymentProofUrl = url;
    }

    public void SetInternalNotes(string? notes) => InternalNotes = notes;

    /// <summary>
    /// Test-support only: constructs an Order in an arbitrary state so
    /// <see cref="TransitionTo"/> can be unit tested before Phase 3's Create() exists.
    /// </summary>
    internal static Order ForTesting(OrderStatus status, FulfillmentType fulfillment) => new()
    {
        Id = "test-order",
        CreatedAt = DateTime.UtcNow,
        Status = status,
        CustomerName = "Test Customer",
        Phone = "0000000000",
        Fulfillment = fulfillment,
        PaymentMethod = PaymentMethod.CashOnDeliveryOrPickup
    };

    public void TransitionTo(OrderStatus target)
    {
        if (!IsLegalTransition(Status, target, Fulfillment))
            throw new InvalidStatusTransitionException(Status, target);

        Status = target;
    }

    /// <summary>
    /// Cancellation is only allowed before the order ships/is ready for pickup —
    /// once it's OutForDelivery, ReadyForPickup, or Completed, cancellation happens
    /// manually/offline, not as a status change in the system (confirmed with the
    /// business owner).
    /// </summary>
    internal static bool IsLegalTransition(OrderStatus from, OrderStatus to, FulfillmentType fulfillment)
    {
        if (to == OrderStatus.Cancelled)
        {
            return from is OrderStatus.PendingConfirmation or OrderStatus.Confirmed or OrderStatus.Processing;
        }

        return (from, to, fulfillment) switch
        {
            (OrderStatus.PendingConfirmation, OrderStatus.Confirmed, _) => true,
            (OrderStatus.Confirmed, OrderStatus.Processing, _) => true,
            (OrderStatus.Processing, OrderStatus.OutForDelivery, FulfillmentType.Delivery) => true,
            (OrderStatus.Processing, OrderStatus.ReadyForPickup, FulfillmentType.Pickup) => true,
            (OrderStatus.OutForDelivery, OrderStatus.Completed, FulfillmentType.Delivery) => true,
            (OrderStatus.ReadyForPickup, OrderStatus.Completed, FulfillmentType.Pickup) => true,
            _ => false
        };
    }
}
