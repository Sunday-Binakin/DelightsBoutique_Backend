using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminNewsletterControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task Subscribers_Unauthenticated_Returns401()
    {
        var response = await PublicClient().GetAsync("/api/admin/newsletter/subscribers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Subscribers_WithWrongPermission_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // ManageOrders, not ManageContent

        var response = await client.GetAsync("/api/admin/newsletter/subscribers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Subscribers_WithManageContentPermission_ListsSubscriber()
    {
        var email = $"admin-list-{Guid.NewGuid():N}@test.local";
        await PublicClient().PostAsync("/api/newsletter/subscribe", new { email });
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);

        var response = await client.GetAsync("/api/admin/newsletter/subscribers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        var emails = body.GetProperty("data").EnumerateArray().Select(s => s.GetProperty("email").GetString()).ToList();
        emails.Should().Contain(email);
    }
}
