using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Products.Commands;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
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

    private const int MaxCsvBytes = 2_097_152; // 2MB — generous for a product CSV

    [HttpPost("bulk-upload")]
    // Kestrel's own ceiling, set above MaxCsvBytes — same reasoning as
    // AdminUploadsController.UploadImage: keeps the explicit size check below reachable
    // for a realistically-oversized file instead of hitting Kestrel's unfixable raw
    // ProblemDetails response (the caveat OrdersController.UploadPaymentProof accepted).
    [RequestSizeLimit(4_194_304)]
    public async Task<IActionResult> BulkUpload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException([new ValidationFailure("file", "file is required.")]);

        if (file.Length > MaxCsvBytes)
            throw new ValidationException([new ValidationFailure("file", "File exceeds 2MB.")]);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await mediator.Send(new BulkUploadProductsCommand(ms.ToArray()), ct);
        return Ok(result);
    }
}
