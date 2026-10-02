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

        var newArrivals = await products.GetBySlugsAsync(updated.NewArrivalsSlugs, cancellationToken);
        var bestSellers = await products.GetBySlugsAsync(updated.BestSellersSlugs, cancellationToken);
        var wholesalePicks = await products.GetBySlugsAsync(updated.WholesalePicksSlugs, cancellationToken);

        return new CollectionSectionDto(
            updated.Eyebrow, updated.Title, updated.NewArrivalsLabel, updated.BestSellersLabel, updated.WholesalePicksLabel,
            newArrivals, bestSellers, wholesalePicks);
    }
}
