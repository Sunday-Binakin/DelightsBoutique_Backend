using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class ProductsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private async Task<(string Slug, AuthTestClient Admin)> CreateTestProductAsync(string namePrefix, decimal retailPrice, string? badge = null)
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Test Category", tone = "Rose" });

        var name = $"{namePrefix} {Guid.NewGuid():N}";
        var body = new
        {
            name, categorySlug, description = "integration test product",
            retailPrice, tone = "Rose", stockStatus = "InStock", badge
        };
        var response = await admin.PostAsync("/api/admin/products", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await TestAuthHelper.ReadJsonAsync(response);
        return (created.GetProperty("slug").GetString()!, admin);
    }

    [Fact]
    public async Task Create_Then_PublicGetBySlug_ReturnsProduct()
    {
        var (slug, _) = await CreateTestProductAsync("Rose Scarf", 50m);

        var response = await PublicClient().GetAsync($"/api/products/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("slug").GetString().Should().Be(slug);
    }

    [Fact]
    public async Task GetBySlug_Unknown_Returns404()
    {
        var response = await PublicClient().GetAsync("/api/products/does-not-exist-slug");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns409()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var slug = $"dup-slug-{Guid.NewGuid():N}"[..20];
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Test Category", tone = "Rose" });

        var body = new { slug, name = "First", categorySlug, description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" };
        (await admin.PostAsync("/api/admin/products", body)).StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await admin.PostAsync("/api/admin/products", body with { });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errorBody = await TestAuthHelper.ReadJsonAsync(secondResponse);
        errorBody.GetProperty("error").GetProperty("code").GetString().Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Create_WithoutManageProductsPermission_Returns403()
    {
        var staffWithoutPermission = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await staffWithoutPermission.PostAsync("/api/admin/products",
            new { name = "X", categorySlug = "whatever", description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await PublicClient().PostAsync("/api/admin/products",
            new { name = "X", categorySlug = "whatever", description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_ByName_UsesCaseInsensitiveMatch()
    {
        var (slug, _) = await CreateTestProductAsync("UniqueSearchTermXYZ", 30m);

        var response = await PublicClient().GetAsync("/api/products?search=uniquesearchtermxyz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        var slugs = body.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().Contain(slug);
    }

    [Fact]
    public async Task Search_SortByPriceAsc_OrdersResultsAscending()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await CreateTestProductAsync($"SortTest{marker}A", 100m);
        await CreateTestProductAsync($"SortTest{marker}B", 10m);
        await CreateTestProductAsync($"SortTest{marker}C", 50m);

        var response = await PublicClient().GetAsync($"/api/products?search=SortTest{marker}&sort=price-asc&limit=10");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var prices = body.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("retailPrice").GetDecimal()).ToList();
        prices.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Update_UnknownSlug_Returns404()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await admin.PatchAsync("/api/admin/products/does-not-exist", new { name = "New Name" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_PartialBody_OnlyChangesSpecifiedFields()
    {
        var (slug, admin) = await CreateTestProductAsync("UpdateMe", 20m);

        await admin.PatchAsync($"/api/admin/products/{slug}", new { retailPrice = 99m });

        var response = await PublicClient().GetAsync($"/api/products/{slug}");
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("retailPrice").GetDecimal().Should().Be(99m);
        body.GetProperty("description").GetString().Should().Be("integration test product"); // untouched
    }

    [Fact]
    public async Task Delete_ThenPublicGet_Returns404()
    {
        var (slug, admin) = await CreateTestProductAsync("DeleteMe", 15m);

        var deleteResponse = await admin.DeleteAsync($"/api/admin/products/{slug}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await PublicClient().GetAsync($"/api/products/{slug}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
