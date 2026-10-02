using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Customers.Commands;
using DelightsBoutique.Application.Customers.Dtos;
using DelightsBoutique.Application.Customers.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/customers")]
[RequirePermission(Permission.ManageCustomers)]
public class AdminCustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] bool? wholesaleOnly,
        [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomersQuery(search, wholesaleOnly, page, limit), ct);
        return Ok(result);
    }

    [HttpGet("{phone}/orders")]
    public async Task<IActionResult> Orders(string phone, CancellationToken ct)
    {
        var orders = await mediator.Send(new GetCustomerOrdersQuery(phone), ct);
        return Ok(new { data = orders });
    }

    [HttpPatch("{phone}")]
    public async Task<IActionResult> SetWholesale(string phone, SetWholesaleBody body, CancellationToken ct)
    {
        var customer = await mediator.Send(new SetCustomerWholesaleCommand(phone, body.Wholesale), ct);
        return customer is null ? NotFound() : Ok(customer);
    }
}
