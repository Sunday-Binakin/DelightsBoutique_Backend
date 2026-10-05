using DelightsBoutique.Application.Reports.Dtos;

namespace DelightsBoutique.Application.Abstractions;

/// <summary>Four independent, simple queries rather than one composed mega-query —
/// deliberately avoids EfCustomerRepository's translation pitfall (Phase 4): a .Where()
/// applied after projecting into a constructed record fails to translate once a query is
/// already composed of several joins/groupings. Each method here ends in exactly one
/// .Select() into its DTO, with no further composition after.</summary>
public interface IReportsRepository
{
    Task<ReportsSummaryDto> GetSummaryAsync(ReportsFilter filter, CancellationToken ct);
    Task<List<TopProductDto>> GetTopProductsAsync(ReportsFilter filter, CancellationToken ct);
    Task<List<StatusBreakdownEntry>> GetStatusBreakdownAsync(ReportsFilter filter, CancellationToken ct);
    Task<List<PaymentBreakdownEntry>> GetPaymentBreakdownAsync(ReportsFilter filter, CancellationToken ct);
    Task<List<DailyRevenueEntry>> GetDailySeriesAsync(ReportsFilter filter, CancellationToken ct);
}
