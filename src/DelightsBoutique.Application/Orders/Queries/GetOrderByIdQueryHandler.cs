using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Queries;

public class GetOrderByIdQueryHandler(IOrderRepository orders) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken) =>
        orders.GetDtoByIdAsync(request.Id, cancellationToken);
}
