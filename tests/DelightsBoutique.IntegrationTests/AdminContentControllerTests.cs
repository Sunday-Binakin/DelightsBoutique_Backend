using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminContentControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private Task<AuthTestClient> ContentAdmin() => TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);

    [Fact]
    public async Task AnyContentEndpoint_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageContent

        var response = await client.PatchAsync("/api/admin/content/site-settings", new { tagline = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSiteSettings_HappyPath()
    {
        var admin = await ContentAdmin();

        var response = await admin.PatchAsync("/api/admin/content/site-settings", new { tagline = $"Tagline {Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReplaceBanner_HappyPath()
    {
        var admin = await ContentAdmin();
        var banner = new { eyebrow = "New", title = "Title", ctaLabel = "Shop", href = "/shop", tone = "Rose", artCategorySlug = "all", imageUrl = (string?)null, textColor = (string?)null };

        var response = await admin.PutAsync("/api/admin/content/banners/heroMain", banner);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReplaceBanner_UnknownKey_Returns404()
    {
        var admin = await ContentAdmin();
        var banner = new { eyebrow = "New", title = "Title", ctaLabel = "Shop", href = "/shop", tone = "Rose", artCategorySlug = "all", imageUrl = (string?)null, textColor = (string?)null };

        var response = await admin.PutAsync("/api/admin/content/banners/notARealSlot", banner);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFlashSale_HappyPath()
    {
        var admin = await ContentAdmin();

        var response = await admin.PatchAsync("/api/admin/content/flash-sale", new { title = $"Sale {Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateCollectionSection_HappyPath()
    {
        var admin = await ContentAdmin();

        var response = await admin.PatchAsync("/api/admin/content/collection-section", new { title = $"Collection {Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStorefrontCopy_HappyPath()
    {
        var admin = await ContentAdmin();

        var response = await admin.PatchAsync("/api/admin/content/storefront-copy", new { someField = "someValue" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReplacePage_HappyPath()
    {
        var admin = await ContentAdmin();

        var response = await admin.PutAsync("/api/admin/content/pages/about", new { title = "About Us", body = "We sell lovely things." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Faq_AddUpdateDelete_FullLifecycle()
    {
        var admin = await ContentAdmin();

        var addResponse = await admin.PostAsync("/api/admin/content/faqs", new { question = "Do you ship nationwide?", answer = "Yes." });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var faqId = (await TestAuthHelper.ReadJsonAsync(addResponse)).GetProperty("id").GetString();

        var updateResponse = await admin.PatchAsync($"/api/admin/content/faqs/{faqId}", new { answer = "Yes, nationwide." });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await admin.DeleteAsync($"/api/admin/content/faqs/{faqId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateFaq_Unknown_Returns404()
    {
        var admin = await ContentAdmin();

        var response = await admin.PatchAsync("/api/admin/content/faqs/does-not-exist", new { answer = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
