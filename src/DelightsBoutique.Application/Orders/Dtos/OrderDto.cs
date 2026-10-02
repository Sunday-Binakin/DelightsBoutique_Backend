using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Orders.Dtos;

public record OrderLineRequest(string Slug, int Quantity, string? Size, string? Color);

public record OrderItemDto(string Slug, string Name, int Quantity, decimal UnitPrice, string? Size, string? Color);

// Structurally has no InternalNotes property — matches BUILD_SPEC exactly. The admin
// OrderDto (with InternalNotes) is Phase 4's.
public record OrderDto(
    string Id, DateTime CreatedAt, OrderStatus Status, List<OrderItemDto> Items, decimal Subtotal,
    string CustomerName, string Phone, FulfillmentType Fulfillment, string? Address,
    PaymentMethod PaymentMethod, string? PaymentProofUrl, string? Notes);

public record CreateOrderBody(
    List<OrderLineRequest> Items, string CustomerName, string Phone,
    FulfillmentType Fulfillment, string? Address, PaymentMethod PaymentMethod, string? Notes);
