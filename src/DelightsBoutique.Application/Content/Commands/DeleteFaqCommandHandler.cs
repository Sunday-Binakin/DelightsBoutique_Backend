using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class DeleteFaqCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<DeleteFaqCommand, bool>
{
    public async Task<bool> Handle(DeleteFaqCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.PagesContent, cancellationToken);
        var pages = JsonSerializer.Deserialize<PagesContentDto>(json!, ContentJsonOptions.Default)!;

        var remaining = pages.Faqs.Where(f => f.Id != request.Id).ToList();
        if (remaining.Count == pages.Faqs.Count)
            return false;

        var updated = pages with { Faqs = remaining };
        await siteContent.UpsertAsync(SiteContentKey.PagesContent, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);
        return true;
    }
}
