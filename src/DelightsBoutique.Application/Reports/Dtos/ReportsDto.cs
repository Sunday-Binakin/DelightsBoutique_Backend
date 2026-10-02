using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Reports.Dtos;

public record ReportsFilter(DateTime? From, DateTime? To, FulfillmentType? Fulfillment, PaymentMethod? PaymentMethod, string? CategorySlug);

// TotalOrders/StatusBreakdown/PaymentBreakdown count every matching order — the whole point
// of a status breakdown is showing how many are Cancelled. ConfirmedRevenue and TopProducts
// additionally exclude Cancelled, same predicate as CustomerFlag's aggregated totalSpent.
public record ReportsSummaryDto(int TotalOrders, int CancelledOrders, decimal ConfirmedRevenue);

public record TopProductDto(string Slug, string Name, int UnitsSold, decimal Revenue);

public record StatusBreakdownEntry(OrderStatus Status, int Count);

public record PaymentBreakdownEntry(PaymentMethod PaymentMethod, int Count);

public record ReportsDto(ReportsSummaryDto Summary, List<TopProductDto> TopProducts, List<StatusBreakdownEntry> StatusBreakdown, List<PaymentBreakdownEntry> PaymentBreakdown);
