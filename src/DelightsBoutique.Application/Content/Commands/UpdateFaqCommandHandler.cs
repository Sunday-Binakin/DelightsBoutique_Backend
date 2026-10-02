using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class UpdateFaqCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<UpdateFaqCommand, FaqItemDto?>
{
    public async Task<FaqItemDto?> Handle(UpdateFaqCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.PagesContent, cancellationToken);
        var pages = JsonSerializer.Deserialize<PagesContentDto>(json!, ContentJsonOptions.Default)!;

        var existing = pages.Faqs.FirstOrDefault(f => f.Id == request.Id);
        if (existing is null)
            return null;

        var updatedFaq = existing with
        {
            Question = request.Question ?? existing.Question,
            Answer = request.Answer ?? existing.Answer
        };

        var updatedFaqs = pages.Faqs.Select(f => f.Id == request.Id ? updatedFaq : f).ToList();
        var updatedPages = pages with { Faqs = updatedFaqs };

        await siteContent.UpsertAsync(SiteContentKey.PagesContent, JsonSerializer.Serialize(updatedPages, ContentJsonOptions.Default), cancellationToken);
        return updatedFaq;
    }
}
