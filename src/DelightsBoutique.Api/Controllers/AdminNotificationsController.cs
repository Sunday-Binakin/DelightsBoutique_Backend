using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Notifications.Commands;
using DelightsBoutique.Application.Notifications.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[RequirePermission(Permission.ManageOrders)]
public class AdminNotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var notifications = await mediator.Send(new GetNotificationsQuery(), ct);
        return Ok(new { data = notifications });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var count = await mediator.Send(new GetUnreadNotificationCountQuery(), ct);
        return Ok(new { count });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var found = await mediator.Send(new MarkNotificationReadCommand(id), ct);
        return found ? NoContent() : NotFound();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return NoContent();
    }
}
