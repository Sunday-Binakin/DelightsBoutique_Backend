using DelightsBoutique.Application.Abstractions;
using MediatR;

namespace DelightsBoutique.Application.Notifications.Commands;

// Deliberately NOT IAuditableCommand — marking a notification read is low-stakes UI state,
// not an admin mutation worth an audit trail entry (same reasoning Phase 3 used for not
// auditing CreateOrderCommand).
public record MarkNotificationReadCommand(Guid Id) : IRequest<bool>;

public class MarkNotificationReadCommandHandler(IOrderNotificationRepository notifications) : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken ct) =>
        notifications.MarkReadAsync(request.Id, ct);
}

public record MarkAllNotificationsReadCommand : IRequest;

public class MarkAllNotificationsReadCommandHandler(IOrderNotificationRepository notifications) : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken ct) =>
        await notifications.MarkAllReadAsync(ct);
}
