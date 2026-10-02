using System.Threading.Channels;
using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Infrastructure.Notifications;

/// <summary>Registered as a singleton (one shared channel for the process lifetime) and
/// bound to both its concrete type and INotificationQueue — writers (handlers) depend only
/// on INotificationQueue; Reader is deliberately not part of that interface, only
/// NotificationQueueProcessor (which needs the concrete type) consumes it.</summary>
public class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationMessage> _channel = Channel.CreateUnbounded<NotificationMessage>();

    public ChannelReader<NotificationMessage> Reader => _channel.Reader;

    public void Enqueue(NotificationMessage message) => _channel.Writer.TryWrite(message);
}
