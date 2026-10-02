using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminProductsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<(AuthTestClient Admin, string CategorySlug)> AdminWithCategory()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Products Test Category", tone = "Rose" });
        return (admin, categorySlug);
    }

    [Fact]
    public async Task Create_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageProducts

        var response = await client.PostAsync("/api/admin/products", new { name = "x", categorySlug = "x", description = "x", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_HappyPath_AutoGeneratesSlugFromName()
    {
        var (admin, categorySlug) = await AdminWithCategory();
        var name = $"Woven Tote Bag {Guid.NewGuid():N}";

        var response = await admin.PostAsync("/api/admin/products", new { name, categorySlug, description = "A lovely bag", retailPrice = 85m, tone = "Rose", stockStatus = "InStock" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("slug").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("retailPrice").GetDecimal().Should().Be(85m);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns409()
    {
        var (admin, categorySlug) = await AdminWithCategory();
        var slug = $"dup-{Guid.NewGuid():N}"[..24];
        await admin.PostAsync("/api/admin/products", new { slug, name = "First", categorySlug, description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" });

        var response = await admin.PostAsync("/api/admin/products", new { slug, name = "Second", categorySlug, description = "d", retailPrice = 20m, tone = "Rose", stockStatus = "InStock" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_HappyPath_ChangesRetailPrice()
    {
        var (admin, categorySlug) = await AdminWithCategory();
        var createResponse = await admin.PostAsync("/api/admin/products", new { name = $"Updatable {Guid.NewGuid():N}", categorySlug, description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" });
        var slug = (await TestAuthHelper.ReadJsonAsync(createResponse)).GetProperty("slug").GetString();

        var response = await admin.PatchAsync($"/api/admin/products/{slug}", new { retailPrice = 15m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("retailPrice").GetDecimal().Should().Be(15m);
    }

    [Fact]
    public async Task Update_Unknown_Returns404()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await admin.PatchAsync("/api/admin/products/does-not-exist", new { retailPrice = 15m });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_HappyPath_RemovesProduct()
    {
        var (admin, categorySlug) = await AdminWithCategory();
        var createResponse = await admin.PostAsync("/api/admin/products", new { name = $"Deletable {Guid.NewGuid():N}", categorySlug, description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock" });
        var slug = (await TestAuthHelper.ReadJsonAsync(createResponse)).GetProperty("slug").GetString();

        var response = await admin.DeleteAsync($"/api/admin/products/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await admin.DeleteAsync("/api/admin/products/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
