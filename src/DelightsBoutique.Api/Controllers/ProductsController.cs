using DelightsBoutique.Application.Products.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category, [FromQuery] string? search, [FromQuery] Badge? badge,
        [FromQuery] string? sort, [FromQuery] int page = 1, [FromQuery] int limit = 20,
        [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null,
        [FromQuery] List<string>? size = null, [FromQuery] List<string>? color = null, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetProductsQuery(category, search, badge, sort, page, limit, minPrice, maxPrice, size, color), ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var product = await mediator.Send(new GetProductBySlugQuery(slug), ct);
        return product is null ? NotFound() : Ok(product);
    }
}
