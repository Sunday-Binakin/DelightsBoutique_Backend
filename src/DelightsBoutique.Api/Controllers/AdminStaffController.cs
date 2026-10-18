using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Staff.Commands;
using DelightsBoutique.Application.Staff.Dtos;
using DelightsBoutique.Application.Staff.Queries;
using DelightsBoutique.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/staff")]
[RequirePermission(Permission.ManageStaff)] // Owner still passes everything — PermissionAuthorizationHandler succeeds on the Owner claim before checking any specific permission
public class AdminStaffController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var members = await mediator.Send(new GetStaffQuery(), ct);
        return Ok(members);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffBody body, CancellationToken ct)
    {
        // Same reasoning as OrdersController.Create: Application never touches HttpContext,
        // so the header is read here before any command object exists.
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
            throw new ValidationException([new ValidationFailure("Idempotency-Key", "Idempotency-Key header is required.")]);

        var command = new CreateStaffCommand(keyValues.First()!, body.Name, body.Email, body.Phone, body.Permissions);
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStaffBody body, CancellationToken ct)
    {
        var command = new UpdateStaffCommand(id, body.Name, body.Email, body.Phone, body.Permissions, body.Active);
        var updated = await mediator.Send(command, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ResetStaffPasswordCommand(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
