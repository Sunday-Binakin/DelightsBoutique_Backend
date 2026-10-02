using DelightsBoutique.Application.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var categories = await mediator.Send(new GetCategoriesQuery(), ct);
        return Ok(categories);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var category = await mediator.Send(new GetCategoryBySlugQuery(slug), ct);
        return category is null ? NotFound() : Ok(category);
    }
}
