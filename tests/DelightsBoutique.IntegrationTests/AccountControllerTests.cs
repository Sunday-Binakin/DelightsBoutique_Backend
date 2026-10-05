using System.Net;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AccountControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient PublicClient() => new(factory.CreateClient());

    [Fact]
    public async Task UpdateProfile_Unauthenticated_Returns401()
    {
        var response = await PublicClient().PatchAsync("/api/account/profile", new { name = "New Name" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var response = await PublicClient().GetAsync("/api/account");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_AsStaff_ReturnsCurrentNameEmailPhoneRole()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);

        var response = await client.GetAsync("/api/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("email").GetString().Should().Be(CustomWebApplicationFactory.ActiveStaffEmail);
        body.GetProperty("role").GetString().Should().Be("Staff");
        body.TryGetProperty("phone", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Get_ReflectsAPriorProfileUpdate_NotJustTheOriginalSeed()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);
        var newPhone = $"024{Random.Shared.Next(1000000, 9999999)}";
        await client.PatchAsync("/api/account/profile", new { phone = newPhone });

        var response = await client.GetAsync("/api/account");

        (await TestAuthHelper.ReadJsonAsync(response)).GetProperty("phone").GetString().Should().Be(newPhone);
    }

    [Fact]
    public async Task UpdateProfile_AsOwner_UpdatesNameAndPhone()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);
        var newName = $"Updated Owner {Guid.NewGuid():N}";

        var response = await client.PatchAsync("/api/account/profile", new { name = newName, phone = "0240000000" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("name").GetString().Should().Be(newName);
        body.GetProperty("phone").GetString().Should().Be("0240000000");
        body.GetProperty("role").GetString().Should().Be("Owner");
    }

    [Fact]
    public async Task UpdateProfile_AsStaff_UpdatesNameOnly_NeverTouchesPermissionsOrEmail()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ActiveStaffEmail);
        var newName = $"Updated Staff {Guid.NewGuid():N}";

        var response = await client.PatchAsync("/api/account/profile", new { name = newName });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("name").GetString().Should().Be(newName);
        body.GetProperty("email").GetString().Should().Be(CustomWebApplicationFactory.ActiveStaffEmail);
        body.GetProperty("role").GetString().Should().Be("Staff");

        // Confirm this staff member can still log in with their original permission afterward —
        // proof UpdateProfile never touched Permissions/Active/PasswordHash.
        var sessionResponse = await client.GetAsync("/api/auth/session");
        (await TestAuthHelper.ReadJsonAsync(sessionResponse)).GetProperty("permissions")[0].GetString().Should().Be("manageOrders");
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageProductsStaffEmail);

        var response = await client.PostAsync("/api/account/change-password", new { currentPassword = "WrongPassword123!", newPassword = "BrandNewPassword123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("fields").GetProperty("currentPassword").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_TooShort_Returns400()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);

        var response = await client.PostAsync("/api/account/change-password", new { currentPassword = CustomWebApplicationFactory.KnownPassword, newPassword = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_HappyPath_AllowsLoginWithNewPasswordAfterward_NotOldOne()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageCustomersStaffEmail);
        const string newPassword = "BrandNewPassword456!";

        var response = await client.PostAsync("/api/account/change-password", new { currentPassword = CustomWebApplicationFactory.KnownPassword, newPassword });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldPasswordLogin = await PublicClient().PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ManageCustomersStaffEmail, password = CustomWebApplicationFactory.KnownPassword });
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLogin = await PublicClient().PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ManageCustomersStaffEmail, password = newPassword });
        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_AsOwner_HappyPath()
    {
        var client = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.OwnerEmail);

        var response = await client.PostAsync("/api/account/change-password", new { currentPassword = CustomWebApplicationFactory.KnownPassword, newPassword = "OwnerNewPassword789!" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
