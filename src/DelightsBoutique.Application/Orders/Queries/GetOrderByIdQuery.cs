using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Queries;

public record GetOrderByIdQuery(string Id) : IRequest<OrderDto?>;
