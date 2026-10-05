using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Application.Reports.Dtos;

public record ReportsFilter(DateTime? From, DateTime? To, FulfillmentType? Fulfillment, PaymentMethod? PaymentMethod, string? CategorySlug);

// TotalOrders/StatusBreakdown/PaymentBreakdown count every matching order — the whole point
// of a status breakdown is showing how many are Cancelled. ConfirmedRevenue and TopProducts
// additionally exclude Cancelled, same predicate as CustomerFlag's aggregated totalSpent.
// PendingAmount/CancelledAmount are convenience aggregates also derivable from
// StatusBreakdown (sum of Subtotal for PendingConfirmation/Cancelled respectively) — kept
// at this top level since those two are the amounts a dashboard card would want first.
public record ReportsSummaryDto(int TotalOrders, int CancelledOrders, decimal ConfirmedRevenue, decimal PendingAmount, decimal CancelledAmount);

/// <summary>Category is null if the product has since been deleted entirely — OrderItem
/// snapshots Slug/Name independent of the product still existing, so this is a best-effort
/// lookup, not guaranteed.</summary>
public record TopProductDto(string Slug, string Name, int UnitsSold, decimal Revenue, string? Category);

// Revenue here is every matching order's Subtotal, same inclusion rule as Count (Cancelled
// included) — this is deliberately the one place Cancelled's own revenue is visible broken
// out by status, which is where ReportsSummaryDto.CancelledAmount's number comes from.
public record StatusBreakdownEntry(OrderStatus Status, int Count, decimal Revenue);

public record PaymentBreakdownEntry(PaymentMethod PaymentMethod, int Count, decimal Revenue);

/// <summary>One entry per calendar day (UTC) that had at least one matching order within
/// filter.From/To — days with zero orders are simply absent, not zero-filled. Revenue
/// excludes Cancelled, same definition as ConfirmedRevenue/TopProducts — a revenue trend
/// line shouldn't be inflated by orders that didn't actually happen.</summary>
public record DailyRevenueEntry(DateOnly Date, int OrderCount, decimal Revenue);

public record ReportsDto(
    ReportsSummaryDto Summary, List<TopProductDto> TopProducts,
    List<StatusBreakdownEntry> StatusBreakdown, List<PaymentBreakdownEntry> PaymentBreakdown,
    List<DailyRevenueEntry> DailySeries);
