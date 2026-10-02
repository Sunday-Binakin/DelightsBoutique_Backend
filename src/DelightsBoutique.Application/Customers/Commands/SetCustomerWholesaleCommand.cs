using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Customers.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Customers.Commands;

public record SetCustomerWholesaleCommand(string Phone, bool Wholesale) : IRequest<CustomerDto?>, IAuditableCommand
{
    public string DescribeAction() => $"Set wholesale={Wholesale} for customer '{Phone}'";
}
