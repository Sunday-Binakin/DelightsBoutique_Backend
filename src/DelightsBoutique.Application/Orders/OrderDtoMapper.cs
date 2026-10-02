using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Orders;

internal static class OrderDtoMapper
{
    public static OrderDto ToDto(Order order) => new(
        order.Id, order.CreatedAt, order.Status,
        order.Items.Select(i => new OrderItemDto(i.Slug, i.Name, i.Quantity, i.UnitPrice, i.Size, i.Color)).ToList(),
        order.Subtotal, order.CustomerName, order.Phone, order.Fulfillment, order.Address,
        order.PaymentMethod, order.PaymentProofUrl, order.Notes);
}
