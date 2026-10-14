using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Reports.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Reports.Queries;

public record GetReportsQuery(DateTime? From, DateTime? To, FulfillmentType? Fulfillment, PaymentMethod? PaymentMethod, string? CategorySlug)
    : IRequest<ReportsDto>;

public class GetReportsQueryHandler(IReportsRepository reports) : IRequestHandler<GetReportsQuery, ReportsDto>
{
    public async Task<ReportsDto> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var filter = new ReportsFilter(request.From, request.To, request.Fulfillment, request.PaymentMethod, request.CategorySlug);

        // Sequential, not Task.WhenAll: all four reads share one scoped DbContext, which
        // is not thread-safe for overlapping operations — running them concurrently would
        // throw "a second operation started on this context before a previous operation
        // completed" at runtime. Four quick sequential round trips is a fine trade for an
        // admin-only reports page.
        var summary = await reports.GetSummaryAsync(filter, ct);
        var topProducts = await reports.GetTopProductsAsync(filter, ct);
        var statusBreakdown = await reports.GetStatusBreakdownAsync(filter, ct);
        var paymentBreakdown = await reports.GetPaymentBreakdownAsync(filter, ct);
        var dailySeries = await reports.GetDailySeriesAsync(filter, ct);

        return new ReportsDto(summary, topProducts, statusBreakdown, paymentBreakdown, dailySeries);
    }
}
