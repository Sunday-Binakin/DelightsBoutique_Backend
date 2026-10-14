using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Newsletter.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

// Newsletter subscriber management sits under the same ManageContent permission as the
// rest of the marketing/content surface (banners, flash sale, site settings) — not worth
// a dedicated permission for one small admin list.
[ApiController]
[Route("api/admin/newsletter")]
[RequirePermission(Permission.ManageContent)]
public class AdminNewsletterController(IMediator mediator) : ControllerBase
{
    [HttpGet("subscribers")]
    public async Task<IActionResult> Subscribers([FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetNewsletterSubscribersQuery(page, limit), ct);
        return Ok(result);
    }
}
