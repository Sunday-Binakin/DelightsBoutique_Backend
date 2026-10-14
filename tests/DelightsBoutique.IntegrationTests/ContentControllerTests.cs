using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class ContentControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task GetBanners_ReturnsSeededDefaults()
    {
        var response = await PublicClient().GetAsync("/api/content/banners");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("heroMain").GetProperty("title").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPage_UnknownKey_Returns404()
    {
        var response = await PublicClient().GetAsync("/api/content/pages/not-a-real-page");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPage_KnownKey_ReturnsPage()
    {
        var response = await PublicClient().GetAsync("/api/content/pages/about");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateSiteSettings_ReflectsInPublicRead_AndNeverLeaksAdminOnlyFields()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);
        var newTagline = $"Updated tagline {Guid.NewGuid():N}";

        var updateResponse = await admin.PatchAsync("/api/admin/content/site-settings", new { tagline = newTagline, notificationEmail = "secret@internal.test" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicResponse = await PublicClient().GetAsync("/api/content/site-settings");
        var body = await TestAuthHelper.ReadJsonAsync(publicResponse);

        body.GetProperty("tagline").GetString().Should().Be(newTagline);
        body.TryGetProperty("notificationEmail", out _).Should().BeFalse("the public DTO must not expose admin-only fields at all");
        body.TryGetProperty("notifyByEmail", out _).Should().BeFalse();
        body.TryGetProperty("notifyByWhatsapp", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSiteSettings_WithoutManageContentPermission_Returns403()
    {
        var wrongPermissionStaff = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await wrongPermissionStaff.PatchAsync("/api/admin/content/site-settings", new { tagline = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StorefrontCopy_ShallowMerge_PreservesUntouchedKeys_RoundTripsThroughRealDb()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);
        var keyA = $"keyA{Guid.NewGuid():N}"[..12];
        var keyB = $"keyB{Guid.NewGuid():N}"[..12];

        await admin.PatchAsync("/api/admin/content/storefront-copy", new Dictionary<string, string> { [keyA] = "first value" });
        var secondUpdate = await admin.PatchAsync("/api/admin/content/storefront-copy", new Dictionary<string, string> { [keyB] = "second value" });
        secondUpdate.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicResponse = await PublicClient().GetAsync("/api/content/storefront-copy");
        var body = await TestAuthHelper.ReadJsonAsync(publicResponse);

        body.GetProperty(keyA).GetString().Should().Be("first value"); // preserved from the first PATCH
        body.GetProperty(keyB).GetString().Should().Be("second value");
    }

    [Fact]
    public async Task Faq_FullLifecycle_AddEditDelete_AgainstRealDb()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);

        var addResponse = await admin.PostAsync("/api/admin/content/faqs", new { question = "Do you ship nationwide?", answer = "Yes." });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var added = await TestAuthHelper.ReadJsonAsync(addResponse);
        var id = added.GetProperty("id").GetString()!;

        var afterAdd = await TestAuthHelper.ReadJsonAsync(await PublicClient().GetAsync("/api/content/faqs"));
        afterAdd.EnumerateArray().Select(f => f.GetProperty("id").GetString()).Should().Contain(id);

        var editResponse = await admin.PatchAsync($"/api/admin/content/faqs/{id}", new { answer = "Yes, nationwide and international." });
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var edited = await TestAuthHelper.ReadJsonAsync(editResponse);
        edited.GetProperty("question").GetString().Should().Be("Do you ship nationwide?"); // untouched
        edited.GetProperty("answer").GetString().Should().Be("Yes, nationwide and international.");

        var deleteResponse = await admin.DeleteAsync($"/api/admin/content/faqs/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await TestAuthHelper.ReadJsonAsync(await PublicClient().GetAsync("/api/content/faqs"));
        afterDelete.EnumerateArray().Select(f => f.GetProperty("id").GetString()).Should().NotContain(id);
    }

    [Fact]
    public async Task Faq_UpdateUnknownId_Returns404()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);

        var response = await admin.PatchAsync("/api/admin/content/faqs/not-a-real-id", new { answer = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FlashSale_ResolvesSlugsToFullProducts()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Flash Sale Category", tone = "Gold" });
        var productResponse = await admin.PostAsync("/api/admin/products", new
        {
            name = $"Flash Sale Product {Guid.NewGuid():N}", categorySlug,
            description = "d", retailPrice = 25m, tone = "Rose", stockStatus = "InStock"
        });
        var productSlug = (await TestAuthHelper.ReadJsonAsync(productResponse)).GetProperty("slug").GetString();

        var contentAdmin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);
        var updateResponse = await contentAdmin.PatchAsync("/api/admin/content/flash-sale", new { productSlugs = new[] { productSlug } });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicResponse = await PublicClient().GetAsync("/api/content/flash-sale");
        var body = await TestAuthHelper.ReadJsonAsync(publicResponse);
        body.GetProperty("isCurated").GetBoolean().Should().BeTrue();
        var products = body.GetProperty("products").EnumerateArray().ToList();
        products.Should().ContainSingle(p => p.GetProperty("slug").GetString() == productSlug);
        products[0].GetProperty("name").GetString().Should().NotBeNullOrEmpty(); // fully resolved, not just the slug
    }

    [Fact]
    public async Task FlashSale_NoCuratedSlugs_AutoFillsFromDiscountedProducts_AndReportsIsCuratedFalse()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Discount Category", tone = "Gold" });
        var productResponse = await admin.PostAsync("/api/admin/products", new
        {
            name = $"Discounted Product {Guid.NewGuid():N}", categorySlug,
            description = "d", retailPrice = 20m, compareAtPrice = 40m, tone = "Rose", stockStatus = "InStock"
        });
        var productSlug = (await TestAuthHelper.ReadJsonAsync(productResponse)).GetProperty("slug").GetString();

        var contentAdmin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);
        await contentAdmin.PatchAsync("/api/admin/content/flash-sale", new { productSlugs = Array.Empty<string>() });

        var publicResponse = await PublicClient().GetAsync("/api/content/flash-sale");
        var body = await TestAuthHelper.ReadJsonAsync(publicResponse);
        body.GetProperty("isCurated").GetBoolean().Should().BeFalse();
        body.GetProperty("products").EnumerateArray().Should().Contain(p => p.GetProperty("slug").GetString() == productSlug);
    }

    [Fact]
    public async Task CollectionSection_NoCuratedSlugs_AutoFillsEachList_AndReportsIsCuratedFalse()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Collection Category", tone = "Gold" });

        var bestSellerResponse = await admin.PostAsync("/api/admin/products", new
        {
            name = $"Best Seller {Guid.NewGuid():N}", categorySlug,
            description = "d", retailPrice = 30m, badge = "BestSeller", tone = "Rose", stockStatus = "InStock"
        });
        var bestSellerSlug = (await TestAuthHelper.ReadJsonAsync(bestSellerResponse)).GetProperty("slug").GetString();

        var wholesaleResponse = await admin.PostAsync("/api/admin/products", new
        {
            name = $"Wholesale Pick {Guid.NewGuid():N}", categorySlug,
            description = "d", retailPrice = 30m, wholesalePrice = 20m, tone = "Rose", stockStatus = "InStock"
        });
        var wholesaleSlug = (await TestAuthHelper.ReadJsonAsync(wholesaleResponse)).GetProperty("slug").GetString();

        var contentAdmin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);
        await contentAdmin.PatchAsync("/api/admin/content/collection-section", new
        {
            newArrivalsSlugs = Array.Empty<string>(),
            bestSellersSlugs = Array.Empty<string>(),
            wholesalePicksSlugs = Array.Empty<string>()
        });

        var publicResponse = await PublicClient().GetAsync("/api/content/collection-section");
        var body = await TestAuthHelper.ReadJsonAsync(publicResponse);

        body.GetProperty("newArrivalsCurated").GetBoolean().Should().BeFalse();
        body.GetProperty("newArrivals").EnumerateArray().Should().NotBeEmpty();

        body.GetProperty("bestSellersCurated").GetBoolean().Should().BeFalse();
        body.GetProperty("bestSellers").EnumerateArray().Should().Contain(p => p.GetProperty("slug").GetString() == bestSellerSlug);

        body.GetProperty("wholesalePicksCurated").GetBoolean().Should().BeFalse();
        body.GetProperty("wholesalePicks").EnumerateArray().Should().Contain(p => p.GetProperty("slug").GetString() == wholesaleSlug);
    }
}
