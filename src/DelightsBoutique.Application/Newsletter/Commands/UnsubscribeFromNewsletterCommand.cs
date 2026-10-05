using DelightsBoutique.Application.Abstractions;
using MediatR;

namespace DelightsBoutique.Application.Newsletter.Commands;

// Deliberately simple — just the email, no signed unsubscribe token. BUILD_SPEC only
// specs POST /subscribe; this endpoint was explicitly called out as optional by the
// frontend. A bare email match means anyone who knows an address can unsubscribe it,
// which is an accepted limitation for a low-stakes marketing list, not sensitive account
// data — the same kind of documented gap as the Kestrel oversized-file caveat elsewhere
// in this API, not an oversight.
public record UnsubscribeFromNewsletterCommand(string Email) : IRequest<bool>;

public class UnsubscribeFromNewsletterCommandHandler(INewsletterRepository newsletter) : IRequestHandler<UnsubscribeFromNewsletterCommand, bool>
{
    public async Task<bool> Handle(UnsubscribeFromNewsletterCommand request, CancellationToken ct)
    {
        var existing = await newsletter.GetByEmailAsync(request.Email, ct);
        if (existing is null)
            return false;

        await newsletter.RemoveAsync(existing, ct);
        return true;
    }
}
