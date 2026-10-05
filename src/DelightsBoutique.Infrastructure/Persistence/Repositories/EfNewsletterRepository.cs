using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Newsletter.Dtos;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence.Repositories;

public class EfNewsletterRepository(AppDbContext db) : INewsletterRepository
{
    public Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.NewsletterSubscribers.FirstOrDefaultAsync(n => n.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(NewsletterSubscriber subscriber, CancellationToken ct)
    {
        db.NewsletterSubscribers.Add(subscriber);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(NewsletterSubscriber subscriber, CancellationToken ct)
    {
        db.NewsletterSubscribers.Remove(subscriber);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<NewsletterSubscriberDto>> SearchAsync(int page, int limit, CancellationToken ct)
    {
        var query = db.NewsletterSubscribers.AsNoTracking()
            .OrderByDescending(n => n.SubscribedAt).ThenBy(n => n.Id); // ThenBy stabilizes paging across ties

        var total = await query.CountAsync(ct);

        var data = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(n => new NewsletterSubscriberDto(n.Email, n.SubscribedAt))
            .ToListAsync(ct);

        return new PagedResult<NewsletterSubscriberDto>(data, page, limit, total);
    }
}
