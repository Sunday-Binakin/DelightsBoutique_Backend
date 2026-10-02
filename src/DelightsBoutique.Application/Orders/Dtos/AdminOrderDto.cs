using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Orders.Dtos;

/// <summary>Admin-facing shape — same as OrderDto plus InternalNotes. Kept as a separate
/// record (not a shared base) to mirror the existing public-vs-admin DTO split already used
/// for SiteSettingsDto/PublicSiteSettingsDto — the public OrderDto structurally cannot leak
/// InternalNotes because the property doesn't exist on that type.</summary>
public record AdminOrderDto(
    string Id, DateTime CreatedAt, OrderStatus Status, List<OrderItemDto> Items, decimal Subtotal,
    string CustomerName, string Phone, FulfillmentType Fulfillment, string? Address,
    PaymentMethod PaymentMethod, string? PaymentProofUrl, string? Notes, string? InternalNotes);

public record OrderSearchFilter(
    OrderStatus? Status, FulfillmentType? Fulfillment, PaymentMethod? PaymentMethod,
    string? CategorySlug, DateTime? From, DateTime? To, string? Search, int Page, int Limit);

public record UpdateOrderStatusBody(OrderStatus? Status, string? InternalNotes);
