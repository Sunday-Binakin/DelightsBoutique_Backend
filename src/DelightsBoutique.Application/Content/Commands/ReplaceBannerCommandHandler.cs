using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class ReplaceBannerCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<ReplaceBannerCommand, BannerSetDto?>
{
    public async Task<BannerSetDto?> Handle(ReplaceBannerCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.Banners, cancellationToken);
        var current = JsonSerializer.Deserialize<BannerSetDto>(json!, ContentJsonOptions.Default)!;

        var updated = request.Key.ToLowerInvariant() switch
        {
            "heromain" => current with { HeroMain = request.Banner },
            "heroside1" => current with { HeroSide1 = request.Banner },
            "heroside2" => current with { HeroSide2 = request.Banner },
            "fullwidth" => current with { FullWidth = request.Banner },
            _ => null
        };

        if (updated is null)
            return null;

        await siteContent.UpsertAsync(SiteContentKey.Banners, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);
        return updated;
    }
}
