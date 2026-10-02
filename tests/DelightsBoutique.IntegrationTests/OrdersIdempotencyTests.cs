using System.Net;
using DelightsBoutique.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.IntegrationTests;

public class OrdersIdempotencyTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    // Unique phone per test — OrdersIdempotencyTests' two methods share one fixture/DB
    // instance (same test class), so a hardcoded phone would let one test's order rows
    // inflate the other's "exactly one order" count assertion.
    private static string UniquePhone() => $"024{Random.Shared.Next(1000000, 9999999)}";

    private static object OrderBody(string slug, int quantity, string phone) => new
    {
        items = new[] { new { slug, quantity } },
        customerName = "Jane Doe", phone,
        fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
    };

    [Fact]
    public async Task CreateOrder_SameKeySameBody_ReturnsIdenticalResponseTwice_AndOnlyOneOrderRowExists()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var key = Guid.NewGuid().ToString();
        var headers = new Dictionary<string, string> { ["Idempotency-Key"] = key };
        var body = OrderBody(slug, 1, UniquePhone());

        var first = await PublicClient().PostAsync("/api/orders", body, headers);
        var second = await PublicClient().PostAsync("/api/orders", body, headers);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstId = (await TestAuthHelper.ReadJsonAsync(first)).GetProperty("id").GetString();
        var secondId = (await TestAuthHelper.ReadJsonAsync(second)).GetProperty("id").GetString();

        // Same Id both times proves the handler didn't re-run — a fresh run would produce
        // a different generated id.
        secondId.Should().Be(firstId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orderCount = await db.Orders.CountAsync(o => o.Id == firstId);
        orderCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateOrder_SameKeyDifferentBody_Returns409_AndNoSecondOrderCreated()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var phone = UniquePhone();
        var key = Guid.NewGuid().ToString();
        var headers = new Dictionary<string, string> { ["Idempotency-Key"] = key };

        var first = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 1, phone), headers);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await PublicClient().PostAsync("/api/orders", OrderBody(slug, 2, phone), headers); // different quantity

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orderCount = await db.Orders.CountAsync(o => o.Phone == phone);
        orderCount.Should().Be(1); // only the first order, never a second
    }
}
