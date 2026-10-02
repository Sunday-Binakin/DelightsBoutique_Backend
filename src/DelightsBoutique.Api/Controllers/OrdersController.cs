using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Application.Orders.Queries;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("order-create")]
    public async Task<IActionResult> Create(CreateOrderBody body, CancellationToken ct)
    {
        // Application must never touch HttpContext (same rule as ICurrentUser), so the
        // header is read here, before any command object exists — matches Fail-Fast, and
        // avoids an ordering wrinkle: IdempotencyBehavior runs before ValidationBehavior,
        // so a validator-level "key required" rule would still let the behavior try to
        // hash/look up an empty key first.
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
            throw new ValidationException([new ValidationFailure("Idempotency-Key", "Idempotency-Key header is required.")]);

        var command = new CreateOrderCommand(
            keyValues.First()!, body.Items, body.CustomerName, body.Phone,
            body.Fulfillment, body.Address, body.PaymentMethod, body.Notes);

        var created = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("track")]
    [EnableRateLimiting("order-track")]
    public async Task<IActionResult> Track([FromQuery] string? phone, CancellationToken ct)
    {
        var data = await mediator.Send(new TrackOrdersByPhoneQuery(phone ?? string.Empty), ct);
        return Ok(new { data });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id), ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id}/payment-proof")]
    [EnableRateLimiting("order-payment-proof")]
    [RequestSizeLimit(5_242_880)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPaymentProof(string id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException([new ValidationFailure("file", "file is required.")]);

        // This manual check is reachable only when IFormFile binding itself didn't already
        // reject the request first. Confirmed empirically: once the multipart body exceeds
        // [RequestSizeLimit], Kestrel's BadHttpRequestException for "body too large" is
        // resolved directly by the host, before it reaches MVC model binding, any controller
        // code, or ExceptionHandlingMiddleware — it surfaces as a generic 400
        // ValidationProblemDetails, never this 413. Known, accepted framework limitation
        // (see BUILD_SPEC conflicts log); not worth a custom IServer-level override for an
        // edge case this minor.
        if (file.Length > 5_242_880)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = new { code = "VALIDATION_ERROR", message = "File exceeds 5MB." } });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var updated = await mediator.Send(new UploadPaymentProofCommand(id, ms.ToArray()), ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
