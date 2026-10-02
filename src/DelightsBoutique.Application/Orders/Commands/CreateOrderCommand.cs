using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

public record CreateOrderCommand(
    string IdempotencyKey, List<OrderLineRequest> Items, string CustomerName, string Phone,
    FulfillmentType Fulfillment, string? Address, PaymentMethod PaymentMethod, string? Notes)
    : IRequest<OrderDto>, IIdempotentCommand;
// Deliberately NOT IAuditableCommand — a customer checkout isn't an attributable admin
// mutation; it would just log actor:"unknown" on every order for no value.
