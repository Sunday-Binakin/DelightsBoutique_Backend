using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record UpdateSiteSettingsCommand(
    string? Tagline, string? WhatsappNumber, string? WhatsappMessage, string? PhoneDisplay,
    string? Address, string? Hours, string? Announcement,
    string? BankTransferInstructions, string? MobileMoneyInstructions, string? CashInstructions,
    string? FacebookUrl, string? InstagramUrl, string? TiktokUrl,
    bool? NotifyByEmail, string? NotificationEmail, bool? NotifyByWhatsapp)
    : IRequest<SiteSettingsDto>, IAuditableCommand
{
    public string DescribeAction() => "Updated site settings";
}
