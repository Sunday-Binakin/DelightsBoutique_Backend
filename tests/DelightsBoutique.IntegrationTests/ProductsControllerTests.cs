using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class ProductsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private async Task<(string Slug, AuthTestClient Admin)> CreateTestProductAsync(
        string namePrefix, decimal retailPrice, string? badge = null, string[]? sizes = null, string[]? colors = null, string? description = null)
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Test Category", tone = "Rose" });

        var name = $"{namePrefix} {Guid.NewGuid():N}";
        var body = new
        {
            name, categorySlug, description = description ?? "integration test product",
            retailPrice, tone = "Rose", stockStatus = "InStock", badge, sizes, colors
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
    public async Task Search_MultiWord_RequiresEachWordSomewhereInNameOrDescription_NotTheLiteralPhrase()
    {
        var marker = $"MwTest{Guid.NewGuid():N}"[..20];
        // Each word appears, but in different fields and not as a contiguous phrase —
        // proves AND-across-words, OR-across-fields, not a literal "word1 word2" substring match.
        var (slug, _) = await CreateTestProductAsync($"{marker}Alpha", 20m, description: $"has the {marker}Beta keyword too");

        var response = await PublicClient().GetAsync($"/api/products?search={marker}Alpha%20{marker}Beta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var slugs = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().Contain(slug);
    }

    [Fact]
    public async Task Search_MultiWord_AllWordsMustMatch_MissingOneExcludesTheProduct()
    {
        var marker = $"MwMiss{Guid.NewGuid():N}"[..20];
        var (slug, _) = await CreateTestProductAsync($"{marker}Only", 20m);

        var response = await PublicClient().GetAsync($"/api/products?search={marker}Only%20{marker}NeverAppearsAnywhere");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var slugs = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().NotContain(slug);
    }

    [Fact]
    public async Task Search_NameMatch_RanksBeforeDescriptionOnlyMatch_EvenWhenOlder()
    {
        var marker = $"RankTest{Guid.NewGuid():N}"[..20];
        var (nameMatchSlug, _) = await CreateTestProductAsync($"{marker}", 20m); // created first == older
        await Task.Delay(50); // CreatedAt granularity safety margin
        var (descriptionOnlySlug, _) = await CreateTestProductAsync("Unrelated", 20m, description: $"mentions {marker} in passing"); // newer

        var response = await PublicClient().GetAsync($"/api/products?search={marker}"); // default sort is newest-first

        var slugs = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.IndexOf(nameMatchSlug).Should().BeLessThan(slugs.IndexOf(descriptionOnlySlug),
            "a Name match should outrank a newer Description-only match despite the default newest-first sort");
    }

    [Fact]
    public async Task Search_MatchesCategoryName_NotJustProductNameOrDescription()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categoryName = $"Category{Guid.NewGuid():N}";
        var categorySlug = categoryName.ToLowerInvariant()[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = categoryName, tone = "Rose" });
        var productResponse = await admin.PostAsync("/api/admin/products", new
        {
            name = "Totally Unrelated Name", categorySlug,
            description = "no overlap with the category name either", retailPrice = 20m, tone = "Rose", stockStatus = "InStock"
        });
        var slug = (await TestAuthHelper.ReadJsonAsync(productResponse)).GetProperty("slug").GetString();

        var response = await PublicClient().GetAsync($"/api/products?search={categoryName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var slugs = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().Contain(slug);
    }

    [Fact]
    public async Task Search_EmptyOrWhitespaceTerm_ReturnsCatalogUnfiltered()
    {
        var unfiltered = await PublicClient().GetAsync("/api/products");
        var withBlankSearch = await PublicClient().GetAsync("/api/products?search=%20");

        var unfilteredTotal = (await TestAuthHelper.ReadJsonAsync(unfiltered)).GetProperty("total").GetInt32();
        var blankTotal = (await TestAuthHelper.ReadJsonAsync(withBlankSearch)).GetProperty("total").GetInt32();
        blankTotal.Should().Be(unfilteredTotal);
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
    public async Task Search_PriceRange_FiltersServerSide_NotJustTheLoadedPage()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await CreateTestProductAsync($"PriceTest{marker}Cheap", 10m);
        var (midSlug, _) = await CreateTestProductAsync($"PriceTest{marker}Mid", 40m);
        await CreateTestProductAsync($"PriceTest{marker}Expensive", 200m);

        var response = await PublicClient().GetAsync($"/api/products?search=PriceTest{marker}&minPrice=30&maxPrice=50");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var slugs = body.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().ContainSingle().Which.Should().Be(midSlug);
        body.GetProperty("total").GetInt32().Should().Be(1); // total reflects the filter, not the unfiltered catalogue
    }

    [Fact]
    public async Task Search_BySize_MatchesProductsWithAnyRequestedSize()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var (mediumSlug, _) = await CreateTestProductAsync($"SizeTest{marker}", 20m, sizes: ["S", "M"]);
        var (largeOnlySlug, _) = await CreateTestProductAsync($"SizeTest{marker}", 20m, sizes: ["XL"]);

        var response = await PublicClient().GetAsync($"/api/products?search=SizeTest{marker}&size=M");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var slugs = body.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().Contain(mediumSlug);
        slugs.Should().NotContain(largeOnlySlug);
    }

    [Fact]
    public async Task Search_ByColor_MatchesProductsWithAnyRequestedColor()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var (redSlug, _) = await CreateTestProductAsync($"ColorTest{marker}", 20m, colors: ["Red", "Blue"]);
        var (greenOnlySlug, _) = await CreateTestProductAsync($"ColorTest{marker}", 20m, colors: ["Green"]);

        var response = await PublicClient().GetAsync($"/api/products?search=ColorTest{marker}&color=Red");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        var slugs = body.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("slug").GetString()).ToList();
        slugs.Should().Contain(redSlug);
        slugs.Should().NotContain(greenOnlySlug);
    }

    [Fact]
    public async Task Search_FilterCombinationWithNoMatches_ReturnsEmptyPage_NotAnError()
    {
        var response = await PublicClient().GetAsync($"/api/products?size={Guid.NewGuid():N}&color={Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
        body.GetProperty("total").GetInt32().Should().Be(0);
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
