using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Newsletter.Dtos;
using DelightsBoutique.Domain.Entities;
using MediatR;

namespace DelightsBoutique.Application.Newsletter.Commands;

public record SubscribeToNewsletterCommand(string Email) : IRequest<SubscribeResult>;

public class SubscribeToNewsletterCommandHandler(INewsletterRepository newsletter) : IRequestHandler<SubscribeToNewsletterCommand, SubscribeResult>
{
    public async Task<SubscribeResult> Handle(SubscribeToNewsletterCommand request, CancellationToken ct)
    {
        var existing = await newsletter.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return new SubscribeResult(IsNew: false, existing.Email, existing.SubscribedAt);

        var subscriber = NewsletterSubscriber.Create(request.Email);
        await newsletter.AddAsync(subscriber, ct);
        return new SubscribeResult(IsNew: true, subscriber.Email, subscriber.SubscribedAt);
    }
}
