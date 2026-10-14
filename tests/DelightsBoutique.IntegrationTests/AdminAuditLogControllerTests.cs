using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminAuditLogControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task List_Unauthenticated_Returns401()
    {
        var response = await PublicClient().GetAsync("/api/admin/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_AsNonOwnerStaff_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.GetAsync("/api/admin/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_AsNonOwnerStaffWithViewAuditLogPermission_Succeeds()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageStaffAndAuditStaffEmail);

        var response = await client.GetAsync("/api/admin/audit-log");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_AfterAdminMutation_ShowsTheEntry_SearchableByAction()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"audit-cat-{Guid.NewGuid():N}"[..24];
        var categoryName = $"Audit Log Test Category {Guid.NewGuid():N}";
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = categoryName, tone = "Rose" });

        var owner = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var response = await owner.GetAsync($"/api/admin/audit-log?search={Uri.EscapeDataString(categorySlug)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
        body.GetProperty("data")[0].GetProperty("actor").GetString().Should().Be(CustomWebApplicationFactory.ManageProductsStaffEmail);
    }

    [Fact]
    public async Task List_NoMatch_ReturnsEmptyData()
    {
        var owner = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);

        var response = await owner.GetAsync($"/api/admin/audit-log?search=no-such-action-{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }
}
