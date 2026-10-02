using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Interfaces;
using DelightsBoutique.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

public class CreateOrderCommandHandler(
    IProductRepository products, ICustomerFlagRepository customerFlags, IPhoneNormalizer phoneNormalizer,
    IOrderIdGenerator orderIdGenerator, IOrderPricingService pricing, IOrderRepository orders,
    IOrderNotificationRepository notifications, ISiteContentRepository siteContent, INotificationQueue notificationQueue)
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
        await EnqueueAdminAlertAsync(order, ct);

        return OrderDtoMapper.ToDto(order);
    }

    // Fire-and-forget enqueue (never awaited downstream) so a slow email/WhatsApp provider
    // can't add latency to the checkout response — the one case in this codebase where
    // "blocking the response" is explicitly the thing being avoided, vs. OTP email which
    // stays synchronous by design.
    private async Task EnqueueAdminAlertAsync(Order order, CancellationToken ct)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.SiteSettings, ct);
        if (json is null) return;

        var settings = JsonSerializer.Deserialize<SiteSettingsDto>(json, ContentJsonOptions.Default)!;

        var message = new NotificationMessage(
            RecipientEmail: settings.NotifyByEmail ? settings.NotificationEmail : null,
            RecipientPhone: settings.NotifyByWhatsapp ? settings.WhatsappNumber : null,
            Subject: $"New order {order.Id}",
            Body: $"<p>New order {order.Id} from {order.CustomerName} — GHS {order.Subtotal:N2}.</p>");

        notificationQueue.Enqueue(message);
    }
}
