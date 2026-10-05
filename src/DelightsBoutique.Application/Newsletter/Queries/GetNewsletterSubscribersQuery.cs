using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Newsletter.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Newsletter.Queries;

public record GetNewsletterSubscribersQuery(int Page = 1, int Limit = 20) : IRequest<PagedResult<NewsletterSubscriberDto>>;

public class GetNewsletterSubscribersQueryHandler(INewsletterRepository newsletter)
    : IRequestHandler<GetNewsletterSubscribersQuery, PagedResult<NewsletterSubscriberDto>>
{
    public Task<PagedResult<NewsletterSubscriberDto>> Handle(GetNewsletterSubscribersQuery request, CancellationToken ct) =>
        newsletter.SearchAsync(request.Page, request.Limit, ct);
}
