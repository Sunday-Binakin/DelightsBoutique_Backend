using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Notifications.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Notifications.Queries;

public record GetNotificationsQuery(int Limit = 50) : IRequest<List<OrderNotificationDto>>;

public class GetNotificationsQueryHandler(IOrderNotificationRepository notifications) : IRequestHandler<GetNotificationsQuery, List<OrderNotificationDto>>
{
    public Task<List<OrderNotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken) =>
        notifications.GetRecentAsync(request.Limit, cancellationToken);
}

public record GetUnreadNotificationCountQuery : IRequest<int>;

public class GetUnreadNotificationCountQueryHandler(IOrderNotificationRepository notifications) : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken) =>
        notifications.GetUnreadCountAsync(cancellationToken);
}
