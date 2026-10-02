using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Queries;

public record TrackOrdersByPhoneQuery(string Phone) : IRequest<List<OrderDto>>;
