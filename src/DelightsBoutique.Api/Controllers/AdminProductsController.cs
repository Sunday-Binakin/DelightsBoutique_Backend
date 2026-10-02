using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Products.Commands;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
[RequirePermission(Permission.ManageProducts)]
public class AdminProductsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductBody body, CancellationToken ct)
    {
        var command = new CreateProductCommand(
            body.Slug, body.Name, body.CategorySlug, body.Description,
            body.RetailPrice, body.WholesalePrice, body.WholesaleMoq, body.CompareAtPrice,
            body.Rating, body.ReviewCount, body.Tone, body.Badge, body.StockStatus,
            body.Sizes, body.Colors, body.ImageUrls);

        var created = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(ProductsController.GetBySlug), "Products", new { slug = created.Slug }, created);
    }

    [HttpPatch("{slug}")]
    public async Task<IActionResult> Update(string slug, UpdateProductBody body, CancellationToken ct)
    {
        var command = new UpdateProductCommand(
            slug, body.Name, body.CategorySlug, body.Description,
            body.RetailPrice, body.WholesalePrice, body.WholesaleMoq, body.CompareAtPrice,
            body.Rating, body.ReviewCount, body.Tone, body.Badge, body.StockStatus,
            body.Sizes, body.Colors, body.ImageUrls);

        var updated = await mediator.Send(command, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> Delete(string slug, CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteProductCommand(slug), ct);
        return deleted ? NoContent() : NotFound();
    }
}
