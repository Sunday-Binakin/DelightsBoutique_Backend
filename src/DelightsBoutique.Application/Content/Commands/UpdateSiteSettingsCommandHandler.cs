using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public class UpdateSiteSettingsCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<UpdateSiteSettingsCommand, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
    {
        var json = await siteContent.GetValueJsonAsync(SiteContentKey.SiteSettings, cancellationToken);
        var current = JsonSerializer.Deserialize<SiteSettingsDto>(json!, ContentJsonOptions.Default)!;

        var updated = current with
        {
            Tagline = request.Tagline ?? current.Tagline,
            WhatsappNumber = request.WhatsappNumber ?? current.WhatsappNumber,
            WhatsappMessage = request.WhatsappMessage ?? current.WhatsappMessage,
            PhoneDisplay = request.PhoneDisplay ?? current.PhoneDisplay,
            Address = request.Address ?? current.Address,
            Hours = request.Hours ?? current.Hours,
            Announcement = request.Announcement ?? current.Announcement,
            BankTransferInstructions = request.BankTransferInstructions ?? current.BankTransferInstructions,
            MobileMoneyInstructions = request.MobileMoneyInstructions ?? current.MobileMoneyInstructions,
            CashInstructions = request.CashInstructions ?? current.CashInstructions,
            FacebookUrl = request.FacebookUrl ?? current.FacebookUrl,
            InstagramUrl = request.InstagramUrl ?? current.InstagramUrl,
            TiktokUrl = request.TiktokUrl ?? current.TiktokUrl,
            NotifyByEmail = request.NotifyByEmail ?? current.NotifyByEmail,
            NotificationEmail = request.NotificationEmail ?? current.NotificationEmail,
            NotifyByWhatsapp = request.NotifyByWhatsapp ?? current.NotifyByWhatsapp
        };

        await siteContent.UpsertAsync(SiteContentKey.SiteSettings, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);
        return updated;
    }
}
