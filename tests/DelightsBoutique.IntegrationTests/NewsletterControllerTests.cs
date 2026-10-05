using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class NewsletterControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task Subscribe_NewEmail_Returns201WithSubscribedAt()
    {
        var email = $"subscriber-{Guid.NewGuid():N}@test.local";

        var response = await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("email").GetString().Should().Be(email);
        body.TryGetProperty("subscribedAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Subscribe_AlreadySubscribed_Returns200WithDuplicateFlag_NotAnError()
    {
        var email = $"subscriber-{Guid.NewGuid():N}@test.local";
        await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });

        var response = await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("duplicate").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Subscribe_IsCaseInsensitiveForDedup()
    {
        var email = $"MixedCase-{Guid.NewGuid():N}@Test.Local";
        await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });

        var response = await PublicClient().PostAsync("/api/newsletter/subscribe", new { email = email.ToUpperInvariant() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("duplicate").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Subscribe_InvalidEmail_Returns400()
    {
        var response = await PublicClient().PostAsync("/api/newsletter/subscribe", new { email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unsubscribe_ExistingEmail_Returns204_AndSubsequentUnsubscribeReturns404()
    {
        var email = $"subscriber-{Guid.NewGuid():N}@test.local";
        await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });

        var response = await PublicClient().DeleteAsync($"/api/newsletter/subscribe?email={email}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondAttempt = await PublicClient().DeleteAsync($"/api/newsletter/subscribe?email={email}");
        secondAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unsubscribe_ThenResubscribe_IsTreatedAsNew_NotDuplicate()
    {
        var email = $"subscriber-{Guid.NewGuid():N}@test.local";
        await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });
        await PublicClient().DeleteAsync($"/api/newsletter/subscribe?email={email}");

        var response = await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
