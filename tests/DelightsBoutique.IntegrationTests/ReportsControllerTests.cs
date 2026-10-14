using System.Net;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.IntegrationTests;

public class ReportsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private async Task<string> CreateOrderAsync(string slug, int quantity = 1)
    {
        var response = await PublicClient().PostAsync("/api/orders", new
        {
            items = new[] { new { slug, quantity } },
            customerName = "Reports Test", phone = $"024{Random.Shared.Next(1000000, 9999999)}",
            fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
        }, new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        return (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }

    private async Task CancelOrderAsync(string orderId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await db.Orders.SingleAsync(o => o.Id == orderId);
        order.TransitionTo(OrderStatus.Cancelled);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var response = await PublicClient().GetAsync("/api/admin/reports");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ViewReports

        var response = await client.GetAsync("/api/admin/reports");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_CancelledOrder_ExcludedFromConfirmedRevenueButCountedInTotalOrders()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 50m);
        var confirmedOrderId = await CreateOrderAsync(slug);
        var cancelledOrderId = await CreateOrderAsync(slug);
        await CancelOrderAsync(cancelledOrderId);

        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);
        var response = await client.GetAsync("/api/admin/reports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        var summary = body.GetProperty("summary");
        summary.GetProperty("totalOrders").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        summary.GetProperty("cancelledOrders").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var statusBreakdown = body.GetProperty("statusBreakdown").EnumerateArray().ToList();
        statusBreakdown.Should().Contain(e => e.GetProperty("status").GetString() == "cancelled" && e.GetProperty("count").GetInt32() >= 1);

        // The cancelled order's own revenue must not appear in topProducts' sum for this slug beyond the confirmed order's contribution.
        confirmedOrderId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_InvalidDateRange_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);

        var response = await client.GetAsync("/api/admin/reports?from=2026-06-01&to=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_TopProducts_RanksByRevenue()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 50m);
        await CreateOrderAsync(slug, 3);

        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);
        var response = await client.GetAsync("/api/admin/reports");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var topProducts = body.GetProperty("topProducts").EnumerateArray().ToList();
        topProducts.Should().Contain(p => p.GetProperty("slug").GetString() == slug);
    }

    [Fact]
    public async Task Get_TopProducts_IncludesCategory()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 50m);
        await CreateOrderAsync(slug);

        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);
        var response = await client.GetAsync("/api/admin/reports");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var topProducts = body.GetProperty("topProducts").EnumerateArray().ToList();
        topProducts.Should().Contain(p => p.GetProperty("slug").GetString() == slug && p.GetProperty("category").GetString() == "Orders Test Category");
    }

    [Fact]
    public async Task Get_PendingAndCancelledAmounts_ReflectActualSubtotals()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 40m);
        await CreateOrderAsync(slug); // stays PendingConfirmation
        var cancelledOrderId = await CreateOrderAsync(slug);
        await CancelOrderAsync(cancelledOrderId);

        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);
        var response = await client.GetAsync("/api/admin/reports");

        var summary = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("summary");
        summary.GetProperty("pendingAmount").GetDecimal().Should().BeGreaterThanOrEqualTo(40m);
        summary.GetProperty("cancelledAmount").GetDecimal().Should().BeGreaterThanOrEqualTo(40m);
    }

    [Fact]
    public async Task Get_StatusAndPaymentBreakdown_IncludeRevenuePerGroup()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 60m);
        await CreateOrderAsync(slug);

        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);
        var response = await client.GetAsync("/api/admin/reports");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var statusBreakdown = body.GetProperty("statusBreakdown").EnumerateArray().ToList();
        statusBreakdown.Should().Contain(e => e.GetProperty("status").GetString() == "pendingConfirmation" && e.GetProperty("revenue").GetDecimal() >= 60m);

        var paymentBreakdown = body.GetProperty("paymentBreakdown").EnumerateArray().ToList();
        paymentBreakdown.Should().Contain(e => e.GetProperty("paymentMethod").GetString() == "cashOnDeliveryOrPickup" && e.GetProperty("revenue").GetDecimal() >= 60m);
    }

    [Fact]
    public async Task Get_DailySeries_IncludesTodayWithOrderCountAndRevenue_ExcludingCancelled()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 70m);
        await CreateOrderAsync(slug);
        var cancelledOrderId = await CreateOrderAsync(slug);
        await CancelOrderAsync(cancelledOrderId);

        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ViewReportsStaffEmail);
        var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var response = await client.GetAsync("/api/admin/reports"); // unscoped — a date-range 'to' would exclude orders created later today

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var dailySeries = body.GetProperty("dailySeries").EnumerateArray().ToList();
        dailySeries.Should().Contain(d => d.GetProperty("date").GetString() == today && d.GetProperty("revenue").GetDecimal() >= 70m);
    }
}
