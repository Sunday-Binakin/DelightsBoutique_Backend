using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

/// <summary>
/// FluentValidation's default PropertyName is the raw C# property (PascalCase), and
/// dictionary keys are never touched by the JSON serializer's camelCase policy — that
/// only applies to reflected property names, not string dictionary keys. Confirms
/// ExceptionHandlingMiddleware camelCases every "fields" key regardless of which
/// validator produced it, flat or nested.
/// </summary>
public class ValidationErrorEnvelopeTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task FlatPropertyValidationFailure_FieldKeyIsCamelCase_NotPascalCase()
    {
        var response = await PublicClient().PostAsync("/api/newsletter/subscribe", new { email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var fields = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("email", out _).Should().BeTrue();
        fields.TryGetProperty("Email", out _).Should().BeFalse();
    }

    [Fact]
    public async Task NestedRuleForEachValidationFailure_FieldKeyIsCamelCase_IncludingTheArrayIndex()
    {
        var response = await PublicClient().PostAsync("/api/orders", new
        {
            items = new[] { new { slug = "", quantity = 0 } },
            customerName = "", phone = "",
            fulfillment = "Pickup", paymentMethod = "CashOnDeliveryOrPickup"
        }, new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var fields = (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("error").GetProperty("fields");
        fields.TryGetProperty("items[0].slug", out _).Should().BeTrue();
        fields.TryGetProperty("items[0].quantity", out _).Should().BeTrue();
        fields.TryGetProperty("customerName", out _).Should().BeTrue();
        fields.TryGetProperty("Items[0].Slug", out _).Should().BeFalse();
    }
}
