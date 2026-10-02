using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public static class OrdersTestHelper
{
    public static async Task<string> CreateTestProductAsync(
        CustomWebApplicationFactory factory, decimal retailPrice,
        decimal? wholesalePrice = null, int? wholesaleMoq = null, string stockStatus = "InStock")
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Orders Test Category", tone = "Rose" });

        var body = new
        {
            name = $"Orders Test Product {Guid.NewGuid():N}", categorySlug,
            description = "order integration test product",
            retailPrice, wholesalePrice, wholesaleMoq, tone = "Rose", stockStatus
        };

        var response = await admin.PostAsync("/api/admin/products", body);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var created = await TestAuthHelper.ReadJsonAsync(response);
        return created.GetProperty("slug").GetString()!;
    }
}
