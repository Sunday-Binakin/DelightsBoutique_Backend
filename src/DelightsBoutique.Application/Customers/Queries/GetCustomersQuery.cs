using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Customers.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Customers.Queries;

public record GetCustomersQuery(string? Search, CustomerType? CustomerType, bool? WholesaleOnly, int Page = 1, int Limit = 20)
    : IRequest<PagedResult<CustomerDto>>;

public class GetCustomersQueryHandler(ICustomerRepository customers) : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken) =>
        customers.SearchAsync(
            new CustomerSearchFilter(request.Search, request.CustomerType, request.WholesaleOnly, request.Page, request.Limit),
            cancellationToken);
}
