using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Newsletter.Dtos;
using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Application.Abstractions;

public interface INewsletterRepository
{
    Task<NewsletterSubscriber?> GetByEmailAsync(string email, CancellationToken ct);

    Task AddAsync(NewsletterSubscriber subscriber, CancellationToken ct);

    /// <summary>Entity already mutated/removed by the caller; saves internally.</summary>
    Task RemoveAsync(NewsletterSubscriber subscriber, CancellationToken ct);

    Task<PagedResult<NewsletterSubscriberDto>> SearchAsync(int page, int limit, CancellationToken ct);
}
