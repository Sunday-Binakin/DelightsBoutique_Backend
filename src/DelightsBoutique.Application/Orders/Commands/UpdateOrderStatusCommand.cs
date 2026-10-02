using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

public record UpdateOrderStatusCommand(string Id, OrderStatus? Status, string? InternalNotes)
    : IRequest<AdminOrderDto?>, IAuditableCommand
{
    public string DescribeAction() => Status is not null
        ? $"Updated order '{Id}' to status '{Status}'"
        : $"Updated order '{Id}' notes";
}
