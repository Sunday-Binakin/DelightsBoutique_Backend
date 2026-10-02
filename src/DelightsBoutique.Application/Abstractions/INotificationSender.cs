namespace DelightsBoutique.Application.Abstractions;

/// <summary>A null RecipientEmail/RecipientPhone means that channel is disabled for this
/// particular message — each INotificationChannel implementation no-ops on its own null
/// recipient field, so "dispatch to both channels together" stays a structural guarantee
/// of the composite while actual per-event channel selection is just data.</summary>
public record NotificationMessage(string? RecipientEmail, string? RecipientPhone, string Subject, string Body);

/// <summary>One delivery channel (email, WhatsApp). Implementations are fanned out to by
/// CompositeNotificationSender — never depended on directly by callers.</summary>
public interface INotificationChannel
{
    Task SendAsync(NotificationMessage message, CancellationToken ct);
}

/// <summary>What callers (order/staff handlers) actually depend on. Bound to
/// CompositeNotificationSender in DI — deliberately a separate interface from
/// INotificationChannel so IEnumerable&lt;INotificationChannel&gt; injected into the
/// composite can never accidentally resolve the composite itself.</summary>
public interface INotificationSender
{
    Task SendAsync(NotificationMessage message, CancellationToken ct);
}

/// <summary>Fire-and-forget enqueue so a slow notification provider never adds latency to
/// the caller's HTTP response. Delivery happens on a background queue; see
/// NotificationQueueProcessor.</summary>
public interface INotificationQueue
{
    void Enqueue(NotificationMessage message);
}
