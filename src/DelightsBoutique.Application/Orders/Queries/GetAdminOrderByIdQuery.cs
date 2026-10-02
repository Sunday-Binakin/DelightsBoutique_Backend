using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Queries;

public record GetAdminOrderByIdQuery(string Id) : IRequest<AdminOrderDto?>;

public class GetAdminOrderByIdQueryHandler(IOrderRepository orders) : IRequestHandler<GetAdminOrderByIdQuery, AdminOrderDto?>
{
    public Task<AdminOrderDto?> Handle(GetAdminOrderByIdQuery request, CancellationToken cancellationToken) =>
        orders.GetAdminDtoByIdAsync(request.Id, cancellationToken);
}
