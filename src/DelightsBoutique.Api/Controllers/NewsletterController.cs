using DelightsBoutique.Application.Newsletter.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController(IMediator mediator) : ControllerBase
{
    public record SubscribeBody(string Email);

    [HttpPost("subscribe")]
    [EnableRateLimiting("newsletter-subscribe")]
    public async Task<IActionResult> Subscribe(SubscribeBody body, CancellationToken ct)
    {
        var result = await mediator.Send(new SubscribeToNewsletterCommand(body.Email), ct);
        return result.IsNew
            ? StatusCode(StatusCodes.Status201Created, new { email = result.Email, subscribedAt = result.SubscribedAt })
            : Ok(new { duplicate = true });
    }

    // Query param, not a DELETE body — some HTTP clients/proxies don't reliably send a
    // body on DELETE, and an unsubscribe link only ever needs the address itself.
    [HttpDelete("subscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string email, CancellationToken ct)
    {
        var removed = await mediator.Send(new UnsubscribeFromNewsletterCommand(email), ct);
        return removed ? NoContent() : NotFound();
    }
}
