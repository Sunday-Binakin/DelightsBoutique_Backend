using System.Net;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.IntegrationTests;

public class PaymentProofUploadTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    // Minimal valid 1x1 transparent PNG, base64-encoded — avoids adding ImageSharp as a
    // test-only dependency just to generate a real image file.
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private static object OrderBody(string slug) => new
    {
        items = new[] { new { slug, quantity = 1 } },
        customerName = "Jane Doe", phone = $"024{Random.Shared.Next(1000000, 9999999)}",
        fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
    };

    private async Task<string> CreateOrderAsync()
    {
        var slug = await OrdersTestHelper.CreateTestProductAsync(factory, retailPrice: 25m);
        var response = await PublicClient().PostAsync("/api/orders", OrderBody(slug),
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        return (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task UploadPaymentProof_HappyPath_SetsUrlAndReturnsOrder()
    {
        var orderId = await CreateOrderAsync();

        var response = await PublicClient().PostMultipartAsync($"/api/orders/{orderId}/payment-proof", MinimalPng);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        var url = body.GetProperty("paymentProofUrl").GetString();
        url.Should().NotBeNullOrEmpty();
        url.Should().Contain("/files/payment-proofs/");
    }

    [Fact]
    public async Task UploadPaymentProof_NonImageContent_Returns400()
    {
        var orderId = await CreateOrderAsync();
        var plainTextBytes = "this is not an image"u8.ToArray();

        // Sent with a misleading image/png multipart Content-Type, deliberately — proves
        // the server sniffs real bytes and doesn't trust the declared type.
        var response = await PublicClient().PostMultipartAsync($"/api/orders/{orderId}/payment-proof", plainTextBytes);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadPaymentProof_OversizedFile_Returns400()
    {
        // Confirmed empirically during manual verification: once the multipart body
        // exceeds [RequestSizeLimit], Kestrel's "body too large" BadHttpRequestException is
        // resolved directly by the host before it reaches MVC model binding or any app code
        // (controller, ExceptionHandlingMiddleware) — it surfaces as a generic
        // ValidationProblemDetails 400, never a custom 413. A true RFC-clean 413 would need
        // a custom IServer-level override; not worth it for this edge case. The explicit
        // file.Length check in the handler still returns 413 for the (narrower) case where a
        // file slips past the attribute but is still over the limit.
        var orderId = await CreateOrderAsync();
        var oversized = new byte[6 * 1024 * 1024];

        var response = await PublicClient().PostMultipartAsync($"/api/orders/{orderId}/payment-proof", oversized);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadPaymentProof_WrongStatus_Returns409()
    {
        var orderId = await CreateOrderAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = await db.Orders.SingleAsync(o => o.Id == orderId);
            order.TransitionTo(OrderStatus.Confirmed);
            await db.SaveChangesAsync();
        }

        var response = await PublicClient().PostMultipartAsync($"/api/orders/{orderId}/payment-proof", MinimalPng);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UploadPaymentProof_UnknownOrderId_Returns404()
    {
        var response = await PublicClient().PostMultipartAsync("/api/orders/does-not-exist/payment-proof", MinimalPng);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
