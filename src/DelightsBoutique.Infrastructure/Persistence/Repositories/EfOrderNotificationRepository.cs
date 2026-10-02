using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfOrderNotificationRepository(AppDbContext db) : IOrderNotificationRepository
{
    public async Task AddAsync(OrderNotification notification, CancellationToken ct)
    {
        db.OrderNotifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }
}
