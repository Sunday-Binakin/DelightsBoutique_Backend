using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Customers.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Customers.Queries;

public record GetCustomerCountsQuery(string? Search) : IRequest<CustomerCountsDto>;

public class GetCustomerCountsQueryHandler(ICustomerRepository customers) : IRequestHandler<GetCustomerCountsQuery, CustomerCountsDto>
{
    public Task<CustomerCountsDto> Handle(GetCustomerCountsQuery request, CancellationToken cancellationToken) =>
        customers.GetCountsAsync(request.Search, cancellationToken);
}
