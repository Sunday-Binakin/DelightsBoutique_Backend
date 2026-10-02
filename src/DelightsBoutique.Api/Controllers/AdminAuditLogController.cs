using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Audit.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/audit-log")]
[RequireOwner]
public class AdminAuditLogController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAuditLogQuery(search, page, limit), ct);
        return Ok(result);
    }
}
