using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Notifications.Dtos;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfOrderNotificationRepository(AppDbContext db) : IOrderNotificationRepository
{
    public async Task AddAsync(OrderNotification notification, CancellationToken ct)
    {
        db.OrderNotifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<OrderNotificationDto>> GetRecentAsync(int limit, CancellationToken ct) =>
        db.OrderNotifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new OrderNotificationDto(n.Id, n.OrderId, n.CustomerName, n.Subtotal, n.CreatedAt, n.Read))
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(CancellationToken ct) =>
        db.OrderNotifications.CountAsync(n => !n.Read, ct);

    public async Task<bool> MarkReadAsync(Guid id, CancellationToken ct)
    {
        var notification = await db.OrderNotifications.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (notification is null) return false;

        notification.MarkRead();
        await db.SaveChangesAsync(ct);
        return true;
    }

    // Bulk update, no per-row business logic beyond setting Read=true — ExecuteUpdateAsync
    // avoids loading every unread row into memory just to flip one flag on each.
    public Task MarkAllReadAsync(CancellationToken ct) =>
        db.OrderNotifications.Where(n => !n.Read).ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true), ct);
}
