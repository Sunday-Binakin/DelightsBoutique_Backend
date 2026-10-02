using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class ReplacePageCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<ReplacePageCommand, PageContentDto?>
{
    public async Task<PageContentDto?> Handle(ReplacePageCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.PagesContent, cancellationToken);
        var current = JsonSerializer.Deserialize<PagesContentDto>(json!, ContentJsonOptions.Default)!;

        var page = new PageContentDto(request.Title, request.Body);
        var updated = PageKeys.With(current, request.Key, page);
        if (updated is null)
            return null;

        await siteContent.UpsertAsync(SiteContentKey.PagesContent, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);
        return page;
    }
}
