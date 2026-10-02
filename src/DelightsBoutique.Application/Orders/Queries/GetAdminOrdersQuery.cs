using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Orders.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Orders.Queries;

public record GetAdminOrdersQuery(
    OrderStatus? Status, FulfillmentType? Fulfillment, PaymentMethod? PaymentMethod,
    string? CategorySlug, DateTime? From, DateTime? To, string? Search, int Page = 1, int Limit = 20)
    : IRequest<PagedResult<AdminOrderDto>>;

public class GetAdminOrdersQueryHandler(IOrderRepository orders) : IRequestHandler<GetAdminOrdersQuery, PagedResult<AdminOrderDto>>
{
    public Task<PagedResult<AdminOrderDto>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken) =>
        orders.SearchAsync(new OrderSearchFilter(
            request.Status, request.Fulfillment, request.PaymentMethod, request.CategorySlug,
            request.From, request.To, request.Search, request.Page, request.Limit), cancellationToken);
}
