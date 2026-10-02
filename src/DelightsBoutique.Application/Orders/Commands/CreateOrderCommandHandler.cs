using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Interfaces;
using DelightsBoutique.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

public class CreateOrderCommandHandler(
    IProductRepository products, ICustomerFlagRepository customerFlags, IPhoneNormalizer phoneNormalizer,
    IOrderIdGenerator orderIdGenerator, IOrderPricingService pricing, IOrderRepository orders,
    IOrderNotificationRepository notifications)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var normalizedPhone = phoneNormalizer.Normalize(request.Phone);
        var customerFlag = await customerFlags.GetAsync(normalizedPhone, ct);
        var isWholesale = customerFlag?.Wholesale ?? false;

        var lines = new List<OrderLineInput>();
        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var product = await products.GetEntityBySlugAsync(item.Slug, ct);
            if (product is null)
                throw new ValidationException([new ValidationFailure($"items[{i}].slug", $"Product '{item.Slug}' does not exist.")]);

            product.EnsureAvailable(); // ProductUnavailableException -> 400

            var unitPrice = pricing.ResolveUnitPrice(product, item.Quantity, isWholesale);
            lines.Add(new OrderLineInput(product.Slug, product.Name, item.Quantity, unitPrice, item.Size, item.Color));
        }

        var order = Order.Create(
            orderIdGenerator.NewId(), lines, request.CustomerName, normalizedPhone,
            request.Fulfillment, request.Address, request.PaymentMethod, request.Notes);

        await orders.AddAsync(order, ct);
        await notifications.AddAsync(OrderNotification.Create(order.Id, order.CustomerName, order.Subtotal), ct);

        return OrderDtoMapper.ToDto(order);
    }
}
