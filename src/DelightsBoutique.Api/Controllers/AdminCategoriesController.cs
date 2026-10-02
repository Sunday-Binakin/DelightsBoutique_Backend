using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Categories.Commands;
using DelightsBoutique.Application.Categories.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
[RequirePermission(Permission.ManageProducts)] // categories sit under Manage Products per BUILD_SPEC §7 — no separate category permission
public class AdminCategoriesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryBody body, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateCategoryCommand(body.Slug, body.Name, body.Tone, body.ImageUrl), ct);
        return CreatedAtAction(nameof(CategoriesController.GetBySlug), "Categories", new { slug = created.Slug }, created);
    }

    [HttpPatch("{slug}")]
    public async Task<IActionResult> Update(string slug, UpdateCategoryBody body, CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateCategoryCommand(slug, body.Name, body.Tone, body.ImageUrl), ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteCategoryCommand(slug), ct);
        return deleted ? NoContent() : NotFound();
    }
}
