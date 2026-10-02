namespace DelightsBoutique.Application.Content.Dtos;

/// <summary>Full admin shape — includes the 3 admin-only notification fields.</summary>
public record SiteSettingsDto(
    string Tagline, string WhatsappNumber, string WhatsappMessage, string PhoneDisplay,
    string Address, string Hours, string Announcement,
    string BankTransferInstructions, string MobileMoneyInstructions, string CashInstructions,
    string FacebookUrl, string InstagramUrl, string TiktokUrl,
    bool NotifyByEmail, string NotificationEmail, bool NotifyByWhatsapp);

/// <summary>Public-safe subset — structurally cannot leak NotifyByEmail/NotificationEmail/NotifyByWhatsapp,
/// since those properties simply don't exist on this type.</summary>
public record PublicSiteSettingsDto(
    string Tagline, string WhatsappNumber, string WhatsappMessage, string PhoneDisplay,
    string Address, string Hours, string Announcement,
    string BankTransferInstructions, string MobileMoneyInstructions, string CashInstructions,
    string FacebookUrl, string InstagramUrl, string TiktokUrl)
{
    public static PublicSiteSettingsDto FromFull(SiteSettingsDto full) => new(
        full.Tagline, full.WhatsappNumber, full.WhatsappMessage, full.PhoneDisplay,
        full.Address, full.Hours, full.Announcement,
        full.BankTransferInstructions, full.MobileMoneyInstructions, full.CashInstructions,
        full.FacebookUrl, full.InstagramUrl, full.TiktokUrl);
}
