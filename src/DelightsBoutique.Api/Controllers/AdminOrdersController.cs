using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Application.Orders.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[RequirePermission(Permission.ManageOrders)]
public class AdminOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] OrderStatus? status, [FromQuery] FulfillmentType? fulfillment, [FromQuery] PaymentMethod? paymentMethod,
        [FromQuery] string? categorySlug, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAdminOrdersQuery(status, fulfillment, paymentMethod, categorySlug, from, to, search, page, limit), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await mediator.Send(new GetAdminOrderByIdQuery(id), ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, UpdateOrderStatusBody body, CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateOrderStatusCommand(id, body.Status, body.InternalNotes), ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
