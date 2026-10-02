using System.Net;
using DelightsBoutique.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.IntegrationTests;

public class AdminOrdersControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private async Task<string> CreateOrderAsync()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var response = await PublicClient().PostAsync("/api/orders", new
        {
            items = new[] { new { slug, quantity = 1 } },
            customerName = "Admin Orders Test", phone = $"024{Random.Shared.Next(1000000, 9999999)}",
            fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
        }, new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        return (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task List_ReturnsOrdersWithoutAuth_Returns401()
    {
        var response = await PublicClient().GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_WithManageOrdersPermission_ReturnsPagedOrders()
    {
        await CreateOrderAsync();
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // has ManageOrders

        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetById_IncludesInternalNotes_UnlikePublicOrderDto()
    {
        var orderId = await CreateOrderAsync();
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.GetAsync($"/api/admin/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("internalNotes");
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.GetAsync("/api/admin/orders/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_LegalStatusTransition_Succeeds()
    {
        var orderId = await CreateOrderAsync();
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.PatchAsync($"/api/admin/orders/{orderId}", new { status = "Confirmed" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("status").GetString().Should().Be("confirmed");
    }

    [Fact]
    public async Task Patch_IllegalStatusTransition_Returns400()
    {
        var orderId = await CreateOrderAsync(); // starts PendingConfirmation
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.PatchAsync($"/api/admin/orders/{orderId}", new { status = "Completed" }); // skips Confirmed/Processing

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_InternalNotesOnly_SetsNotesWithoutChangingStatus()
    {
        var orderId = await CreateOrderAsync();
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.PatchAsync($"/api/admin/orders/{orderId}", new { internalNotes = "Called customer" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("status").GetString().Should().Be("pendingConfirmation");
        body.GetProperty("internalNotes").GetString().Should().Be("Called customer");
    }

    [Fact]
    public async Task Patch_NeitherStatusNorNotes_Returns400()
    {
        var orderId = await CreateOrderAsync();
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.PatchAsync($"/api/admin/orders/{orderId}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConcurrentStatusUpdates_SecondSaveThrowsConcurrencyException()
    {
        // Proves Order's Postgres xmin concurrency token actually works against the real
        // test database — two separate DbContext scopes both load the same row (same
        // starting xmin), the first save succeeds and changes the row's xmin server-side,
        // and the second scope's save (holding the now-stale xmin) must fail. This is
        // tested at the repository/EF level rather than via two genuinely racing HTTP
        // requests, since reliably forcing two HTTP requests to overlap in a deterministic,
        // non-flaky way isn't practical — the guarantee this proves (the token itself
        // works) is the same either way; ExceptionHandlingMiddleware's catch clause mapping
        // DbUpdateConcurrencyException -> 409 is a one-line, directly-reviewable mapping
        // consistent with every other exception type already handled there.
        var orderId = await CreateOrderAsync();

        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

        var order1 = await db1.Orders.FirstAsync(o => o.Id == orderId);
        var order2 = await db2.Orders.FirstAsync(o => o.Id == orderId);

        order1.TransitionTo(DelightsBoutique.Domain.Enums.OrderStatus.Confirmed);
        await db1.SaveChangesAsync();

        order2.TransitionTo(DelightsBoutique.Domain.Enums.OrderStatus.Confirmed);
        var act = () => db2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
