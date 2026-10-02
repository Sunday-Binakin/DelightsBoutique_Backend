using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminNotificationsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private async Task<string> CreateOrderAsync()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var response = await PublicClient().PostAsync("/api/orders", new
        {
            items = new[] { new { slug, quantity = 1 } },
            customerName = "Notification Test", phone = $"024{Random.Shared.Next(1000000, 9999999)}",
            fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
        }, new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        return (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task List_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.GetAsync("/api/admin/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatingOrder_CreatesUnreadNotification_ReflectedInUnreadCount()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);
        var beforeCount = (await TestAuthHelper.ReadJsonAsync(await client.GetAsync("/api/admin/notifications/unread-count"))).GetProperty("count").GetInt32();

        await CreateOrderAsync();

        var afterCount = (await TestAuthHelper.ReadJsonAsync(await client.GetAsync("/api/admin/notifications/unread-count"))).GetProperty("count").GetInt32();
        afterCount.Should().Be(beforeCount + 1);
    }

    [Fact]
    public async Task MarkRead_UnknownId_Returns404()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.PatchAsync($"/api/admin/notifications/{Guid.NewGuid()}/read");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkRead_ExistingNotification_DropsUnreadCount()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);
        await CreateOrderAsync();
        var list = await TestAuthHelper.ReadJsonAsync(await client.GetAsync("/api/admin/notifications"));
        var notificationId = list.GetProperty("data")[0].GetProperty("id").GetString();
        var beforeCount = (await TestAuthHelper.ReadJsonAsync(await client.GetAsync("/api/admin/notifications/unread-count"))).GetProperty("count").GetInt32();

        var response = await client.PatchAsync($"/api/admin/notifications/{notificationId}/read");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterCount = (await TestAuthHelper.ReadJsonAsync(await client.GetAsync("/api/admin/notifications/unread-count"))).GetProperty("count").GetInt32();
        afterCount.Should().Be(beforeCount - 1);
    }

    [Fact]
    public async Task MarkAllRead_SetsUnreadCountToZero()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);
        await CreateOrderAsync();
        await CreateOrderAsync();

        var response = await client.PostAsync("/api/admin/notifications/mark-all-read");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var count = (await TestAuthHelper.ReadJsonAsync(await client.GetAsync("/api/admin/notifications/unread-count"))).GetProperty("count").GetInt32();
        count.Should().Be(0);
    }
}
