using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IOrderNotificationRepository
{
    Task AddAsync(OrderNotification notification, CancellationToken ct);
}
