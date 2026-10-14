using System.Net;
using System.Text;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminProductsBulkUploadTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Header = "slug,name,categorySlug,description,retailPrice,wholesalePrice,wholesaleMoq,compareAtPrice,tone,badge,stockStatus,sizes,colors,imageUrls";

    [Fact]
    public async Task BulkUpload_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageProducts

        var response = await client.PostMultipartAsync("/api/admin/products/bulk-upload", Encoding.UTF8.GetBytes(Header), fileName: "products.csv", contentType: "text/csv");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BulkUpload_MissingFile_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.PostAsync("/api/admin/products/bulk-upload", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BulkUpload_MixOfValidAndInvalidRows_ReturnsPerRowSummary_NotAllOrNothing()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"bulk-cat-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Bulk Upload Category", tone = "Rose" });

        var goodSlug = $"bulk-good-{Guid.NewGuid():N}"[..24];
        var csv = new StringBuilder()
            .AppendLine(Header)
            .AppendLine($"{goodSlug},Good Product,{categorySlug},A fine product,50,,,,Rose,,InStock,,,")
            .AppendLine($",Bad Price Product,{categorySlug},Has a bad price,not-a-number,,,,Rose,,InStock,,,")
            .ToString();

        var response = await admin.PostMultipartAsync("/api/admin/products/bulk-upload", Encoding.UTF8.GetBytes(csv), fileName: "products.csv", contentType: "text/csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("successCount").GetInt32().Should().Be(1);
        body.GetProperty("errorCount").GetInt32().Should().Be(1);

        var rows = body.GetProperty("rows").EnumerateArray().ToList();
        rows.Should().Contain(r => r.GetProperty("success").GetBoolean() && r.GetProperty("slug").GetString() == goodSlug);
        rows.Should().Contain(r => !r.GetProperty("success").GetBoolean() && r.GetProperty("error").GetString()!.Contains("retailPrice"));

        // Confirm the good row was actually persisted, not just reported as a success.
        var getResponse = await admin.GetAsync($"/api/products/{goodSlug}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BulkUpload_DuplicateSlugAgainstExistingProduct_ReportsRowFailure()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var existingSlug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 10m);
        var categorySlug = $"bulk-cat2-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Bulk Upload Category 2", tone = "Rose" });

        var csv = new StringBuilder()
            .AppendLine(Header)
            .AppendLine($"{existingSlug},Duplicate,{categorySlug},desc,50,,,,Rose,,InStock,,,")
            .ToString();

        var response = await admin.PostMultipartAsync("/api/admin/products/bulk-upload", Encoding.UTF8.GetBytes(csv), fileName: "products.csv", contentType: "text/csv");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("errorCount").GetInt32().Should().Be(1);
        body.GetProperty("rows")[0].GetProperty("error").GetString().Should().Contain(existingSlug);
    }

    [Fact]
    public async Task BulkUpload_OversizedFile_Returns400WithStandardEnvelope_NotRawProblemDetails()
    {
        // Mirrors AdminUploadsControllerTests' oversized-file case: the hard Kestrel
        // ceiling (4MB) sits above the 2MB this endpoint actually enforces, so a
        // realistically-oversized file reaches the explicit check and gets the standard
        // envelope instead of a raw ProblemDetails body.
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var oversized = new byte[3 * 1024 * 1024];

        var response = await client.PostMultipartAsync("/api/admin/products/bulk-upload", oversized, fileName: "big.csv", contentType: "text/csv");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        body.GetProperty("error").GetProperty("fields").GetProperty("file").GetString().Should().Contain("2MB");
    }

    [Fact]
    public async Task BulkUpload_ExceedsMaxRowCount_Returns400_NoRowsPersisted()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var categorySlug = $"bulk-cat3-{Guid.NewGuid():N}"[..20];
        await admin.PostAsync("/api/admin/categories", new { slug = categorySlug, name = "Bulk Upload Category 3", tone = "Rose" });

        var csv = new StringBuilder().AppendLine(Header);
        for (var i = 0; i < 1001; i++)
            csv.AppendLine($"too-many-rows-{Guid.NewGuid():N},Product {i},{categorySlug},desc,10,,,,Rose,,InStock,,,");

        var response = await admin.PostMultipartAsync("/api/admin/products/bulk-upload", Encoding.UTF8.GetBytes(csv.ToString()), fileName: "products.csv", contentType: "text/csv");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("fields").GetProperty("file").GetString().Should().Contain("1000");
    }
}
