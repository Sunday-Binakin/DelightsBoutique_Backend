using DelightsBoutique.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DelightsBoutique.Infrastructure.Notifications;

/// <summary>Fans out one message to every registered INotificationChannel. One channel's
/// failure is caught and logged independently so it never blocks another channel's send —
/// an email-provider outage shouldn't also suppress the WhatsApp notification.</summary>
public class CompositeNotificationSender(IEnumerable<INotificationChannel> channels, ILogger<CompositeNotificationSender> logger) : INotificationSender
{
    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        foreach (var channel in channels)
        {
            try
            {
                await channel.SendAsync(message, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification channel {Channel} failed to send '{Subject}'", channel.GetType().Name, message.Subject);
            }
        }
    }
}
