using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Uploads.Commands;
using DelightsBoutique.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/uploads")]
// Called from both the Products and Content admin forms — accepts either permission rather
// than needing two separate routes for the same endpoint.
[RequirePermission(Permission.ManageProducts, Permission.ManageContent)]
public class AdminUploadsController(IMediator mediator) : ControllerBase
{
    private const int MaxFileBytes = 5_242_880; // 5MB, matching every other upload limit in this API

    [HttpPost("images")]
    // Kestrel's own hard ceiling, set well above MaxFileBytes: a file between 5MB and this
    // limit still reaches the explicit check below and gets the standard error envelope: a
    // file bound via IFormFile is read by MVC's own model binding before this method body
    // ever runs, so once that read itself hits a body-size limit, the resulting
    // BadHttpRequestException is resolved directly by the host as a generic
    // ValidationProblemDetails — unreachable by any try/catch in this action or by
    // ExceptionHandlingMiddleware (confirmed empirically building payment-proof upload in
    // Phase 3). Keeping Kestrel's own limit well above the limit we actually want to
    // enforce means that hard-to-customize failure path is only hit by genuinely abusive
    // payloads, not by "a slightly-too-large real photo" — the realistic case this was
    // asked to fix.
    [RequestSizeLimit(10_485_760)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException([new ValidationFailure("file", "file is required.")]);

        if (file.Length > MaxFileBytes)
            throw new ValidationException([new ValidationFailure("file", "File exceeds 5MB.")]);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await mediator.Send(new UploadImageCommand(ms.ToArray()), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
