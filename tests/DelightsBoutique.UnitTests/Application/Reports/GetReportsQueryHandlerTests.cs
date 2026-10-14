using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Reports.Dtos;
using DelightsBoutique.Application.Reports.Queries;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Reports;

public class GetReportsQueryHandlerTests
{
    private readonly Mock<IReportsRepository> _reports = new();

    private GetReportsQueryHandler CreateHandler() => new(_reports.Object);

    [Fact]
    public async Task Handle_AssemblesAllFiveSectionsFromTheRepository()
    {
        var summary = new ReportsSummaryDto(10, 2, 500m, 100m, 50m);
        var topProducts = new List<TopProductDto> { new("slug-1", "Product One", 5, 250m, "Dresses") };
        var statusBreakdown = new List<StatusBreakdownEntry> { new(OrderStatus.Cancelled, 2, 50m) };
        var paymentBreakdown = new List<PaymentBreakdownEntry> { new(PaymentMethod.MobileMoney, 10, 500m) };
        var dailySeries = new List<DailyRevenueEntry> { new(new DateOnly(2026, 1, 1), 3, 150m) };

        _reports.Setup(r => r.GetSummaryAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(summary);
        _reports.Setup(r => r.GetTopProductsAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(topProducts);
        _reports.Setup(r => r.GetStatusBreakdownAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(statusBreakdown);
        _reports.Setup(r => r.GetPaymentBreakdownAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(paymentBreakdown);
        _reports.Setup(r => r.GetDailySeriesAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(dailySeries);

        var result = await CreateHandler().Handle(new GetReportsQuery(null, null, null, null, null), CancellationToken.None);

        result.Summary.Should().Be(summary);
        result.TopProducts.Should().BeEquivalentTo(topProducts);
        result.StatusBreakdown.Should().BeEquivalentTo(statusBreakdown);
        result.PaymentBreakdown.Should().BeEquivalentTo(paymentBreakdown);
        result.DailySeries.Should().BeEquivalentTo(dailySeries);
    }

    [Fact]
    public async Task Handle_PassesQueryFiltersThroughToTheRepository()
    {
        _reports.Setup(r => r.GetSummaryAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ReportsSummaryDto(0, 0, 0m, 0m, 0m));
        _reports.Setup(r => r.GetTopProductsAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _reports.Setup(r => r.GetStatusBreakdownAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _reports.Setup(r => r.GetPaymentBreakdownAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _reports.Setup(r => r.GetDailySeriesAsync(It.IsAny<ReportsFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var query = new GetReportsQuery(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, FulfillmentType.Delivery, PaymentMethod.BankTransfer, "cat-1");
        await CreateHandler().Handle(query, CancellationToken.None);

        _reports.Verify(r => r.GetSummaryAsync(
            It.Is<ReportsFilter>(f => f.From == query.From && f.To == query.To && f.Fulfillment == query.Fulfillment && f.PaymentMethod == query.PaymentMethod && f.CategorySlug == query.CategorySlug),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
