using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Queries;

public class TrackOrdersByPhoneQueryHandler(IOrderRepository orders, IPhoneNormalizer phoneNormalizer)
    : IRequestHandler<TrackOrdersByPhoneQuery, List<OrderDto>>
{
    public Task<List<OrderDto>> Handle(TrackOrdersByPhoneQuery request, CancellationToken cancellationToken) =>
        orders.GetByPhoneAsync(phoneNormalizer.Normalize(request.Phone), cancellationToken);
}
