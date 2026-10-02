using DelightsBoutique.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DelightsBoutique.Infrastructure.Notifications;

/// <summary>Stand-in until a real WhatsApp Business API / provider is set up — logs instead
/// of sending. Swap this registration for a real INotificationChannel implementation later;
/// no other code (CompositeNotificationSender, callers) needs to change.</summary>
public class NoopWhatsAppNotificationSender(ILogger<NoopWhatsAppNotificationSender> logger) : INotificationChannel
{
    public Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(message.RecipientPhone))
            logger.LogInformation("WhatsApp notification (noop — no provider configured) to {Phone}: {Subject}", message.RecipientPhone, message.Subject);

        return Task.CompletedTask;
    }
}
