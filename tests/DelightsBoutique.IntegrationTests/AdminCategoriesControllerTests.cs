using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

// Create/delete/409-while-referenced/404-unknown are already exercised in
// CategoriesControllerTests.cs (as setup for the public-side tests) — this file covers what
// that one doesn't: Update, duplicate-slug-on-create, and the permission boundary.
public class AdminCategoriesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task List_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageProducts

        var response = await client.PostAsync("/api/admin/categories", new { slug = "x", name = "x", tone = "Rose" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_DuplicateSlug_Returns409()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var slug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug, name = "First", tone = "Rose" });

        var response = await admin.PostAsync("/api/admin/categories", new { slug, name = "Second", tone = "Gold" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_HappyPath_ChangesName()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var slug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug, name = "Original Name", tone = "Rose" });

        var response = await admin.PatchAsync($"/api/admin/categories/{slug}", new { name = "Updated Name" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("name").GetString().Should().Be("Updated Name");
    }

    [Fact]
    public async Task Update_Unknown_Returns404()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await admin.PatchAsync("/api/admin/categories/does-not-exist", new { name = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
