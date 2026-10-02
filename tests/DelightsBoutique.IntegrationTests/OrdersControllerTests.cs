using System.Net;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.IntegrationTests;

public class OrdersControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private static object OrderBody(string slug, int quantity, string phone = "0241234567") => new
    {
        items = new[] { new { slug, quantity } },
        customerName = "Jane Doe",
        phone,
        fulfillment = "Pickup",
        paymentMethod = "CashOnDeliveryOrPickup"
    };

    private static Dictionary<string, string> IdempotencyHeader() => new() { ["Idempotency-Key"] = Guid.NewGuid().ToString() };

    [Fact]
    public async Task CreateOrder_HappyPath_Returns201WithComputedSubtotal()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 3), IdempotencyHeader());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("subtotal").GetDecimal().Should().Be(75m);
        body.GetProperty("status").GetString().Should().Be("pendingConfirmation");
    }

    [Fact]
    public async Task CreateOrder_MissingIdempotencyKeyHeader_Returns400()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_UnresolvableSlug_Returns400()
    {
        var response = await PublicClient().PostAsync("/api/orders", OrderBody("does-not-exist-slug", 1), IdempotencyHeader());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_OutOfStockLine_Returns400()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m, stockStatus: "OutOfStock");

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 1), IdempotencyHeader());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_DeliveryWithoutAddress_Returns400()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var body = new
        {
            items = new[] { new { slug, quantity = 1 } },
            customerName = "Jane Doe", phone = "0241234567",
            fulfillment = "Delivery", paymentMethod = "CashOnDeliveryOrPickup"
        }; // no address

        var response = await PublicClient().PostAsync("/api/orders", body, IdempotencyHeader());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_RetailCustomer_UsesRetailPrice()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 100m, wholesalePrice: 70m, wholesaleMoq: 10);
        var phone = $"024{Random.Shared.Next(1000000, 9999999)}";

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 20, phone), IdempotencyHeader());

        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal().Should().Be(100m); // not flagged wholesale
    }

    [Fact]
    public async Task CreateOrder_WholesaleCustomerAtOrAboveMoq_UsesWholesalePrice()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 100m, wholesalePrice: 70m, wholesaleMoq: 10);
        var phone = $"024{Random.Shared.Next(1000000, 9999999)}";
        await SeedWholesaleFlagAsync(phone);

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 10, phone), IdempotencyHeader());

        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal().Should().Be(70m);
    }

    [Fact]
    public async Task CreateOrder_WholesaleCustomerBelowMoq_UsesRetailPrice()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 100m, wholesalePrice: 70m, wholesaleMoq: 10);
        var phone = $"024{Random.Shared.Next(1000000, 9999999)}";
        await SeedWholesaleFlagAsync(phone);

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 5, phone), IdempotencyHeader());

        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task CreateOrder_CreatesOrderNotificationRow()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);

        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 1), IdempotencyHeader());
        var orderId = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("id").GetString();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db.OrderNotifications.AnyAsync(n => n.OrderId == orderId);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrderById_Unknown_Returns404()
    {
        var response = await PublicClient().GetAsync("/api/orders/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_Known_ResponseHasNoInternalNotesProperty()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var createResponse = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 1), IdempotencyHeader());
        var orderId = (await TestAuthHelper.ReadJsonAsync(createResponse)).GetProperty("id").GetString();

        var response = await PublicClient().GetAsync($"/api/orders/{orderId}");

        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain("internalNotes");
    }

    private async Task SeedWholesaleFlagAsync(string phone)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CustomerFlags.Add(CustomerFlag.ForTesting(phone, wholesale: true));
        await db.SaveChangesAsync();
    }
}
