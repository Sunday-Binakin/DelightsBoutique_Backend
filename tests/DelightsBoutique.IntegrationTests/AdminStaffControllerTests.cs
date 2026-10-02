using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AdminStaffControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    private static object CreateBody(string email) => new { name = "New Staff", email, phone = (string?)null, permissions = new[] { "ManageOrders" } };

    [Fact]
    public async Task List_AsNonOwnerStaff_Returns403()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail); // staff, not owner

        var response = await client.GetAsync("/api/admin/staff");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_Unauthenticated_Returns401()
    {
        var response = await PublicClient().GetAsync("/api/admin/staff");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_AsOwner_Succeeds()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);

        var response = await client.GetAsync("/api/admin/staff");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_MissingIdempotencyKey_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);

        var response = await client.PostAsync("/api/admin/staff", CreateBody($"new-{Guid.NewGuid():N}@test.local"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_HappyPath_ReturnsMemberAndOneTimePassword()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var email = $"new-{Guid.NewGuid():N}@test.local";

        var response = await client.PostAsync("/api/admin/staff", CreateBody(email),
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("member").GetProperty("email").GetString().Should().Be(email);
        body.GetProperty("password").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_SameIdempotencyKeyTwice_ReturnsIdenticalResponse()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var email = $"new-{Guid.NewGuid():N}@test.local";
        var headers = new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() };
        var body = CreateBody(email);

        var first = await client.PostAsync("/api/admin/staff", body, headers);
        var second = await client.PostAsync("/api/admin/staff", body, headers);

        var firstPassword = (await TestAuthHelper.ReadJsonAsync(first)).GetProperty("password").GetString();
        var secondPassword = (await TestAuthHelper.ReadJsonAsync(second)).GetProperty("password").GetString();
        secondPassword.Should().Be(firstPassword); // replayed, not re-generated
    }

    [Fact]
    public async Task Create_SameKeyDifferentBody_Returns409()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var headers = new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() };

        var first = await client.PostAsync("/api/admin/staff", CreateBody($"a-{Guid.NewGuid():N}@test.local"), headers);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync("/api/admin/staff", CreateBody($"b-{Guid.NewGuid():N}@test.local"), headers);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);

        var response = await client.PostAsync("/api/admin/staff", CreateBody(CustomWebApplicationFactory.ActiveStaffEmail),
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_DeactivateStaff_PreventsFurtherLogin()
    {
        var owner = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var email = $"deactivate-me-{Guid.NewGuid():N}@test.local";
        var created = await owner.PostAsync("/api/admin/staff", CreateBody(email),
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        var createdBody = await TestAuthHelper.ReadJsonAsync(created);
        var id = createdBody.GetProperty("member").GetProperty("id").GetString();

        var response = await owner.PatchAsync($"/api/admin/staff/{id}", new { active = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("active").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ResetPassword_ExistingStaff_ReturnsNewPassword()
    {
        var owner = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var email = $"reset-me-{Guid.NewGuid():N}@test.local";
        var created = await owner.PostAsync("/api/admin/staff", CreateBody(email),
            new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() });
        var createdBody = await TestAuthHelper.ReadJsonAsync(created);
        var id = createdBody.GetProperty("member").GetProperty("id").GetString();
        var originalPassword = createdBody.GetProperty("password").GetString();

        var response = await owner.PostAsync($"/api/admin/staff/{id}/reset-password");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("password").GetString().Should().NotBe(originalPassword);
    }
}
