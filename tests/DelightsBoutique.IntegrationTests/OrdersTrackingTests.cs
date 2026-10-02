using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class OrdersTrackingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private static object OrderBody(string slug, string phone) => new
    {
        items = new[] { new { slug, quantity = 1 } },
        customerName = "Jane Doe", phone,
        fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
    };

    [Fact]
    public async Task TrackByPhone_PlusCountryCodeAnd233PrefixedAndZeroPrefixed_AllResolveToSameOrders()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var localDigits = Random.Shared.Next(1000000, 9999999);
        var zeroPrefixed = $"024{localDigits}";

        var createResponse = await PublicClient().PostAsync("/api/orders", OrderBody(slug, zeroPrefixed),
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = (await TestAuthHelper.ReadJsonAsync(createResponse)).GetProperty("id").GetString();

        var plusPrefixed = $"+233{zeroPrefixed[1..]}";
        var noPlusPrefixed = $"233{zeroPrefixed[1..]}";

        foreach (var queryPhone in new[] { zeroPrefixed, plusPrefixed, noPlusPrefixed })
        {
            var response = await PublicClient().GetAsync($"/api/orders/track?phone={Uri.EscapeDataString(queryPhone)}");
            var body = await TestAuthHelper.ReadJsonAsync(response);
            var ids = body.GetProperty("data").EnumerateArray().Select(o => o.GetProperty("id").GetString()).ToList();
            ids.Should().Contain(orderId, $"querying with phone form '{queryPhone}' should resolve to the same order");
        }
    }

    [Fact]
    public async Task TrackByPhone_NoMatch_ReturnsEmptyDataArray_Never404()
    {
        var response = await PublicClient().GetAsync("/api/orders/track?phone=0200000000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }
}
