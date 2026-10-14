using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminCustomersControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private async Task<string> CreateOrderAsync(string phone, decimal retailPrice = 25m)
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: retailPrice);
        var response = await PublicClient().PostAsync("/api/orders", new
        {
            items = new[] { new { slug, quantity = 1 } },
            customerName = "Customer Test", phone,
            fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
        }, new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        return (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task List_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // has ManageOrders, not ManageCustomers

        var response = await client.GetAsync("/api/admin/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_ReturnsAggregatedCustomerAcrossMultipleOrders()
    {
        var phone = $"024{Random.Shared.Next(1000000, 9999999)}";
        await CreateOrderAsync(phone, 25m);
        await CreateOrderAsync(phone, 75m);
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);

        var response = await client.GetAsync($"/api/admin/customers?search={phone}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        var row = body.GetProperty("data")[0];
        row.GetProperty("phone").GetString().Should().Be(phone);
        row.GetProperty("orderCount").GetInt32().Should().Be(2);
        row.GetProperty("totalSpent").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task Orders_ReturnsAllOrdersForPhone()
    {
        var phone = $"024{Random.Shared.Next(1000000, 9999999)}";
        await CreateOrderAsync(phone);
        await CreateOrderAsync(phone);
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);

        var response = await client.GetAsync($"/api/admin/customers/{phone}/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SetWholesale_ExistingCustomer_UpdatesFlag()
    {
        var phone = $"024{Random.Shared.Next(1000000, 9999999)}";
        await CreateOrderAsync(phone);
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);

        var response = await client.PatchAsync($"/api/admin/customers/{phone}", new { wholesale = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("wholesale").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SetWholesale_PhoneWithNoOrders_Returns404()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);

        var response = await client.PatchAsync("/api/admin/customers/0200000099", new { wholesale = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_CustomerTypeRetail_ExcludesWholesaleCustomers()
    {
        var marker = Random.Shared.Next(1000000, 9999999).ToString();
        var retailPhone = $"024{marker}1";
        var wholesalePhone = $"024{marker}2";
        await CreateOrderAsync(retailPhone);
        await CreateOrderAsync(wholesalePhone);
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);
        await client.PatchAsync($"/api/admin/customers/{wholesalePhone}", new { wholesale = true });

        var response = await client.GetAsync($"/api/admin/customers?search={marker}&customerType=retail");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var phones = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("data")
            .EnumerateArray().Select(x => x.GetProperty("phone").GetString()).ToList();
        phones.Should().Contain(retailPhone);
        phones.Should().NotContain(wholesalePhone);
    }

    [Fact]
    public async Task List_CustomerTypeWholesale_OnlyReturnsWholesaleCustomers()
    {
        var marker = Random.Shared.Next(1000000, 9999999).ToString();
        var retailPhone = $"024{marker}1";
        var wholesalePhone = $"024{marker}2";
        await CreateOrderAsync(retailPhone);
        await CreateOrderAsync(wholesalePhone);
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);
        await client.PatchAsync($"/api/admin/customers/{wholesalePhone}", new { wholesale = true });

        var response = await client.GetAsync($"/api/admin/customers?search={marker}&customerType=wholesale");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var phones = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("data")
            .EnumerateArray().Select(x => x.GetProperty("phone").GetString()).ToList();
        phones.Should().Contain(wholesalePhone);
        phones.Should().NotContain(retailPhone);
    }

    [Fact]
    public async Task List_InvalidCustomerType_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);

        var response = await client.GetAsync("/api/admin/customers?customerType=notarealtype");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Counts_ReturnsTotalRetailAndWholesaleBreakdown()
    {
        var marker = Random.Shared.Next(1000000, 9999999).ToString();
        var retailPhone = $"024{marker}1";
        var wholesalePhone = $"024{marker}2";
        await CreateOrderAsync(retailPhone);
        await CreateOrderAsync(wholesalePhone);
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);
        await client.PatchAsync($"/api/admin/customers/{wholesalePhone}", new { wholesale = true });

        var response = await client.GetAsync($"/api/admin/customers/counts?search={marker}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("total").GetInt32().Should().Be(2);
        body.GetProperty("retail").GetInt32().Should().Be(1);
        body.GetProperty("wholesale").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Counts_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageCustomers

        var response = await client.GetAsync("/api/admin/customers/counts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
