using DelightsBoutique.Application.Content.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/content")]
public class ContentController(IMediator mediator) : ControllerBase
{
    [HttpGet("site-settings")]
    public async Task<IActionResult> GetSiteSettings(CancellationToken ct) => Ok(await mediator.Send(new GetSiteSettingsQuery(), ct));

    [HttpGet("banners")]
    public async Task<IActionResult> GetBanners(CancellationToken ct) => Ok(await mediator.Send(new GetBannersQuery(), ct));

    [HttpGet("flash-sale")]
    public async Task<IActionResult> GetFlashSale(CancellationToken ct) => Ok(await mediator.Send(new GetFlashSaleQuery(), ct));

    [HttpGet("collection-section")]
    public async Task<IActionResult> GetCollectionSection(CancellationToken ct) => Ok(await mediator.Send(new GetCollectionSectionQuery(), ct));

    [HttpGet("storefront-copy")]
    public async Task<IActionResult> GetStorefrontCopy(CancellationToken ct) => Ok(await mediator.Send(new GetStorefrontCopyQuery(), ct));

    [HttpGet("pages/{key}")]
    public async Task<IActionResult> GetPage(string key, CancellationToken ct)
    {
        var page = await mediator.Send(new GetPageQuery(key), ct);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpGet("faqs")]
    public async Task<IActionResult> GetFaqs(CancellationToken ct) => Ok(await mediator.Send(new GetFaqsQuery(), ct));
}
