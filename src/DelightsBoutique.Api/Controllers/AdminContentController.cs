using System.Text.Json;
using DelightsBoutique.Api.Filters;
using DelightsBoutique.Application.Content.Commands;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Application.Content.Queries;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/admin/content")]
[RequirePermission(Permission.ManageContent)]
public class AdminContentController(IMediator mediator) : ControllerBase
{
    [HttpGet("site-settings")]
    public async Task<IActionResult> GetSiteSettings(CancellationToken ct) =>
        Ok(await mediator.Send(new GetAdminSiteSettingsQuery(), ct));

    [HttpPatch("site-settings")]
    public async Task<IActionResult> UpdateSiteSettings(UpdateSiteSettingsCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPut("banners/{key}")]
    public async Task<IActionResult> ReplaceBanner(string key, BannerDto body, CancellationToken ct)
    {
        var updated = await mediator.Send(new ReplaceBannerCommand(key, body), ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("flash-sale")]
    public async Task<IActionResult> UpdateFlashSale(UpdateFlashSaleCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPatch("collection-section")]
    public async Task<IActionResult> UpdateCollectionSection(UpdateCollectionSectionCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPatch("storefront-copy")]
    public async Task<IActionResult> UpdateStorefrontCopy([FromBody] JsonElement body, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdateStorefrontCopyCommand(body), ct));

    [HttpPut("pages/{key}")]
    public async Task<IActionResult> ReplacePage(string key, ReplacePageBody body, CancellationToken ct)
    {
        var updated = await mediator.Send(new ReplacePageCommand(key, body.Title, body.Body), ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("faqs")]
    public async Task<IActionResult> AddFaq(AddFaqCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPatch("faqs/{id}")]
    public async Task<IActionResult> UpdateFaq(string id, UpdateFaqBody body, CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateFaqCommand(id, body.Question, body.Answer), ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("faqs/{id}")]
    public async Task<IActionResult> DeleteFaq(string id, CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteFaqCommand(id), ct);
        return deleted ? NoContent() : NotFound();
    }
}
