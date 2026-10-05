using System.Net;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DelightsBoutique.IntegrationTests;

public class AdminUploadsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    // Minimal valid 1x1 transparent PNG — same literal used by PaymentProofUploadTests.
    private static readonly byte[] SmallPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private static byte[] LargeImageBytes(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task UploadImage_Unauthenticated_Returns401()
    {
        var response = await PublicClient().PostMultipartAsync("/api/admin/uploads/images", SmallPng, fileName: "test.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadImage_WithManageOrdersOnly_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageProducts/ManageContent

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", SmallPng, fileName: "test.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadImage_WithManageProductsPermission_Succeeds()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", SmallPng, fileName: "test.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UploadImage_WithManageContentPermission_Succeeds()
    {
        // Confirms the "any of" permission logic — the same route accepts either
        // ManageProducts or ManageContent, not just the first one tested above.
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", SmallPng, fileName: "test.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UploadImage_HappyPath_ReturnsUrlAndDimensions()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", SmallPng, fileName: "test.png", contentType: "image/png");

        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("url").GetString().Should().Contain("/files/uploads/");
        body.GetProperty("width").GetInt32().Should().Be(1); // 1x1 source, no resize triggered
        body.GetProperty("height").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task UploadImage_LargerThan1000px_IsResizedToFitWithAspectRatioPreserved()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var large = LargeImageBytes(2000, 1500); // 4:3

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", large, fileName: "large.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("width").GetInt32().Should().Be(1000);
        body.GetProperty("height").GetInt32().Should().Be(750); // 4:3 preserved
    }

    [Fact]
    public async Task UploadImage_NonImageContent_Returns400WithStandardEnvelope()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var notAnImage = "this is not an image"u8.ToArray();

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", notAnImage, fileName: "fake.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        body.GetProperty("error").GetProperty("fields").GetProperty("file").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadImage_OversizedFile_Returns400WithStandardEnvelope_NotRawProblemDetails()
    {
        // The whole point of R6: unlike payment-proof upload (Phase 3's accepted gap), this
        // endpoint's hard Kestrel ceiling (10MB) sits well above the 5MB limit it actually
        // enforces, so a realistically-oversized file (here, 6MB) reaches the explicit
        // check and gets the standard envelope instead of a raw ProblemDetails body.
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var oversized = new byte[6 * 1024 * 1024];

        var response = await client.PostMultipartAsync("/api/admin/uploads/images", oversized, fileName: "big.png", contentType: "image/png");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        body.GetProperty("error").GetProperty("fields").GetProperty("file").GetString().Should().Contain("5MB");
    }
}
