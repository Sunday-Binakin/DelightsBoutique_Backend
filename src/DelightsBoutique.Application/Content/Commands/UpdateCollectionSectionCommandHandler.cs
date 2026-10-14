using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class UpdateCollectionSectionCommandHandler(ISiteContentRepository siteContent, IProductRepository products)
    : IRequestHandler<UpdateCollectionSectionCommand, CollectionSectionDto>
{
    // Matches GetCollectionSectionQueryHandler's own cap — this handler's response must
    // stay consistent with what a follow-up GET would return.
    private const int AutoFillLimit = 8;

    public async Task<CollectionSectionDto> Handle(UpdateCollectionSectionCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.CollectionSection, cancellationToken);
        var current = JsonSerializer.Deserialize<CollectionSectionContentDto>(json!, ContentJsonOptions.Default)!;

        var updated = current with
        {
            Eyebrow = request.Eyebrow ?? current.Eyebrow,
            Title = request.Title ?? current.Title,
            NewArrivalsLabel = request.NewArrivalsLabel ?? current.NewArrivalsLabel,
            BestSellersLabel = request.BestSellersLabel ?? current.BestSellersLabel,
            WholesalePicksLabel = request.WholesalePicksLabel ?? current.WholesalePicksLabel,
            NewArrivalsSlugs = request.NewArrivalsSlugs ?? current.NewArrivalsSlugs,
            BestSellersSlugs = request.BestSellersSlugs ?? current.BestSellersSlugs,
            WholesalePicksSlugs = request.WholesalePicksSlugs ?? current.WholesalePicksSlugs
        };

        await siteContent.UpsertAsync(SiteContentKey.CollectionSection, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);

        var newArrivalsCurated = updated.NewArrivalsSlugs.Count > 0;
        var newArrivals = newArrivalsCurated
            ? await products.GetBySlugsAsync(updated.NewArrivalsSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.Newest, AutoFillLimit, cancellationToken);

        var bestSellersCurated = updated.BestSellersSlugs.Count > 0;
        var bestSellers = bestSellersCurated
            ? await products.GetBySlugsAsync(updated.BestSellersSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.BestSeller, AutoFillLimit, cancellationToken);

        var wholesalePicksCurated = updated.WholesalePicksSlugs.Count > 0;
        var wholesalePicks = wholesalePicksCurated
            ? await products.GetBySlugsAsync(updated.WholesalePicksSlugs, cancellationToken)
            : await products.GetAutoFillAsync(ProductAutoFillRule.WholesaleEligible, AutoFillLimit, cancellationToken);

        return new CollectionSectionDto(
            updated.Eyebrow, updated.Title, updated.NewArrivalsLabel, updated.BestSellersLabel, updated.WholesalePicksLabel,
            newArrivals, newArrivalsCurated, bestSellers, bestSellersCurated, wholesalePicks, wholesalePicksCurated);
    }
}
