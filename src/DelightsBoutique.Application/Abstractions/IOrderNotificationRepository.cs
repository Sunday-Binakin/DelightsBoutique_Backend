using DelightsBoutique.Application.Notifications.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface IOrderNotificationRepository
{
    Task AddAsync(OrderNotification notification, CancellationToken ct);

    /// <summary>Newest-first, capped at limit.</summary>
    Task<List<OrderNotificationDto>> GetRecentAsync(int limit, CancellationToken ct);

    Task<int> GetUnreadCountAsync(CancellationToken ct);

    /// <summary>False if no notification with this id exists.</summary>
    Task<bool> MarkReadAsync(Guid id, CancellationToken ct);

    Task MarkAllReadAsync(CancellationToken ct);
}
