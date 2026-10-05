namespace DelightsBoutique.Application.Customers.Dtos;

public record CustomerDto(string Phone, string Name, int OrderCount, decimal TotalSpent, DateTime LastOrderAt, bool Wholesale);

// Not a stored attribute (see CustomerFlag's Wholesale bool) — purely a filter-shape
// concept, so it lives here rather than in Domain.Enums alongside Badge/Tone/StockStatus.
public enum CustomerType
{
    Retail,
    Wholesale
}

/// <summary>
/// WholesaleOnly is kept only for backward compatibility with callers that predate
/// CustomerType — when both are given, CustomerType wins. WholesaleOnly could only ever
/// express "wholesale or everyone," never "retail only," which is exactly the gap
/// CustomerType closes.
/// </summary>
public record CustomerSearchFilter(string? Search, CustomerType? CustomerType, bool? WholesaleOnly, int Page, int Limit);

public record CustomerCountsDto(int Total, int Retail, int Wholesale);

public record SetWholesaleBody(bool Wholesale);
