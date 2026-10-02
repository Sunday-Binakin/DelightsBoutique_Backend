using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class UpdateFlashSaleCommandHandler(ISiteContentRepository siteContent, IProductRepository products)
    : IRequestHandler<UpdateFlashSaleCommand, FlashSaleDto>
{
    public async Task<FlashSaleDto> Handle(UpdateFlashSaleCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.FlashSale, cancellationToken);
        var current = JsonSerializer.Deserialize<FlashSaleContentDto>(json!, ContentJsonOptions.Default)!;

        var updated = current with
        {
            Eyebrow = request.Eyebrow ?? current.Eyebrow,
            Title = request.Title ?? current.Title,
            EndsAt = request.EndsAt ?? current.EndsAt,
            ProductSlugs = request.ProductSlugs ?? current.ProductSlugs
        };

        await siteContent.UpsertAsync(SiteContentKey.FlashSale, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);

        var resolved = await products.GetBySlugsAsync(updated.ProductSlugs, cancellationToken);
        return new FlashSaleDto(updated.Eyebrow, updated.Title, updated.EndsAt, resolved);
    }
}
