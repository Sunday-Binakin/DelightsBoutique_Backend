using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class CategoriesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task Create_Then_PublicList_IncludesIt()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var slug = $"cat-{Guid.NewGuid():N}"[..20];

        var createResponse = await admin.PostAsync("/api/admin/categories", new { slug, name = "Integration Category", tone = "Gold" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await PublicClient().GetAsync("/api/categories");
        var body = await TestAuthHelper.ReadJsonAsync(listResponse);
        var slugs = body.EnumerateArray().Select(c => c.GetProperty("slug").GetString()).ToList();
        slugs.Should().Contain(slug);
    }

    [Fact]
    public async Task Delete_WhileReferencedByProduct_Returns409_ThenSucceedsAfterProductRemoved()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "In Use Category", tone = "Charcoal" });

        var productResponse = await admin.PostAsync("/api/admin/products", new
        {
            name = $"Referencing Product {Guid.NewGuid():N}", categorySlug,
            description = "d", retailPrice = 10m, tone = "Rose", stockStatus = "InStock"
        });
        var productSlug = (await TestAuthHelper.ReadJsonAsync(productResponse)).GetProperty("slug").GetString();

        var blockedDelete = await admin.DeleteAsync($"/api/admin/categories/{categorySlug}");
        blockedDelete.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errorBody = await TestAuthHelper.ReadJsonAsync(blockedDelete);
        errorBody.GetProperty("error").GetProperty("code").GetString().Should().Be("CONFLICT");

        (await admin.DeleteAsync($"/api/admin/products/{productSlug}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var successfulDelete = await admin.DeleteAsync($"/api/admin/categories/{categorySlug}");
        successfulDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await admin.DeleteAsync("/api/admin/categories/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
