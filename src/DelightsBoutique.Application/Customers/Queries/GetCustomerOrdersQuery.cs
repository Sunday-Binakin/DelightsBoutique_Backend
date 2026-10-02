using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Customers.Queries;

public record GetCustomerOrdersQuery(string Phone) : IRequest<List<OrderDto>>;

public class GetCustomerOrdersQueryHandler(IOrderRepository orders, IPhoneNormalizer phoneNormalizer)
    : IRequestHandler<GetCustomerOrdersQuery, List<OrderDto>>
{
    public Task<List<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken) =>
        orders.GetByPhoneAsync(phoneNormalizer.Normalize(request.Phone), cancellationToken);
}
