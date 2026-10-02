using DelightsBoutique.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DelightsBoutique.Infrastructure.Notifications;

/// <summary>Drains NotificationQueue in the background so a slow email/WhatsApp provider
/// never adds latency to the request that enqueued the message (e.g. checkout). Resolves a
/// new DI scope per message — this service itself is singleton-lifetime, but
/// INotificationSender and its channels are scoped.</summary>
public class NotificationQueueProcessor(NotificationQueue queue, IServiceScopeFactory scopeFactory, ILogger<NotificationQueueProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
                await sender.SendAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                // One bad message must never kill the processing loop.
                logger.LogError(ex, "Failed to process queued notification '{Subject}'", message.Subject);
            }
        }
    }
}
