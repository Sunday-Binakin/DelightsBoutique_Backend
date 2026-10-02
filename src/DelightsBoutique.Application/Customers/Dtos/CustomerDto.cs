namespace DelightsBoutique.Application.Customers.Dtos;

public record CustomerDto(string Phone, string Name, int OrderCount, decimal TotalSpent, DateTime LastOrderAt, bool Wholesale);

public record CustomerSearchFilter(string? Search, bool? WholesaleOnly, int Page, int Limit);

public record SetWholesaleBody(bool Wholesale);
