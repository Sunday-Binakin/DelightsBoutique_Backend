using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class AddFaqCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<AddFaqCommand, FaqItemDto>
{
    public async Task<FaqItemDto> Handle(AddFaqCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.PagesContent, cancellationToken);
        var pages = JsonSerializer.Deserialize<PagesContentDto>(json!, ContentJsonOptions.Default)!;

        var faq = new FaqItemDto(Guid.NewGuid().ToString("N"), request.Question, request.Answer);
        var updated = pages with { Faqs = [.. pages.Faqs, faq] };

        await siteContent.UpsertAsync(SiteContentKey.PagesContent, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);
        return faq;
    }
}
