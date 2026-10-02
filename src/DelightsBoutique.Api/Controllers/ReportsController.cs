using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Reports.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[RequirePermission(Permission.ViewReports)]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] FulfillmentType? fulfillment,
        [FromQuery] PaymentMethod? paymentMethod, [FromQuery] string? categorySlug, CancellationToken ct)
    {
        var result = await mediator.Send(new GetReportsQuery(from, to, fulfillment, paymentMethod, categorySlug), ct);
        return Ok(result);
    }
}
