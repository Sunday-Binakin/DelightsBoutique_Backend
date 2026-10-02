namespace DelightsBoutique.Application.Notifications.Dtos;

public record OrderNotificationDto(Guid Id, string OrderId, string CustomerName, decimal Subtotal, DateTime CreatedAt, bool Read);
