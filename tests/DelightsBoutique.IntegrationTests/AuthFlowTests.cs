using System.Net;
using System.Text.Json;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;

namespace DelightsBoutique.IntegrationTests;

public class AuthFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private AuthTestClient CreateClient() => new(factory.CreateClient());

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

    [Fact]
    public async Task Login_VerifyOtp_Session_FullFlow_Succeeds()
    {
        var client = CreateClient();

        var loginResponse = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.OwnerEmail, password = CustomWebApplicationFactory.KnownPassword });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await ReadJsonAsync(loginResponse);
        var loginToken = loginBody.GetProperty("loginToken").GetString();

        var sentCode = factory.OtpEmailSender.SentCodes.Single(c => c.Email == CustomWebApplicationFactory.OwnerEmail).Code;

        var verifyResponse = await client.PostAsync("/api/auth/verify-otp", new { loginToken, code = sentCode });
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyBody = await ReadJsonAsync(verifyResponse);
        verifyBody.GetProperty("role").GetString().Should().Be("Owner");
        // Owner bypasses every permission check — the frontend needs the full set reported
        // so it can render the same admin UI it would for a staff member holding everything,
        // rather than special-casing "Owner" as a UI concept of its own.
        verifyBody.GetProperty("permissions").EnumerateArray().Should().HaveCount(Enum.GetValues<Permission>().Length);

        var sessionResponse = await client.GetAsync("/api/auth/session");
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessionBody = await ReadJsonAsync(sessionResponse);
        sessionBody.GetProperty("email").GetString().Should().Be(CustomWebApplicationFactory.OwnerEmail);
        sessionBody.GetProperty("permissions").EnumerateArray().Should().HaveCount(Enum.GetValues<Permission>().Length);
    }

    [Fact]
    public async Task Login_VerifyOtp_Session_Staff_ReportsOnlyTheirOwnPermissions()
    {
        var client = CreateClient();

        var loginResponse = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ManageProductsStaffEmail, password = CustomWebApplicationFactory.KnownPassword });
        var loginToken = (await ReadJsonAsync(loginResponse)).GetProperty("loginToken").GetString();
        var code = factory.OtpEmailSender.SentCodes.Last(c => c.Email == CustomWebApplicationFactory.ManageProductsStaffEmail).Code;

        var verifyResponse = await client.PostAsync("/api/auth/verify-otp", new { loginToken, code });
        var verifyBody = await ReadJsonAsync(verifyResponse);
        var verifyPermissions = verifyBody.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();
        verifyPermissions.Should().BeEquivalentTo(["manageProducts"]); // camelCase, matching every other enum this API returns

        var sessionResponse = await client.GetAsync("/api/auth/session");
        var sessionBody = await ReadJsonAsync(sessionResponse);
        var sessionPermissions = sessionBody.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();
        sessionPermissions.Should().BeEquivalentTo(["manageProducts"]);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.OwnerEmail, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_DeactivatedStaff_Returns403WithStaffDeactivatedCode()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.DeactivatedStaffEmail, password = CustomWebApplicationFactory.KnownPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("STAFF_DEACTIVATED");
    }

    [Fact]
    public async Task VerifyOtp_WrongCode_Returns401()
    {
        var client = CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ActiveStaffEmail, password = CustomWebApplicationFactory.KnownPassword });
        var loginToken = (await ReadJsonAsync(loginResponse)).GetProperty("loginToken").GetString();

        var response = await client.PostAsync("/api/auth/verify-otp", new { loginToken, code = "000000" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyOtp_ExpiredCode_Returns401()
    {
        var client = CreateClient();
        var originalTime = factory.Clock.UtcNow;
        var loginResponse = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ActiveStaffEmail, password = CustomWebApplicationFactory.KnownPassword });
        var loginToken = (await ReadJsonAsync(loginResponse)).GetProperty("loginToken").GetString();
        var code = factory.OtpEmailSender.SentCodes.Last(c => c.Email == CustomWebApplicationFactory.ActiveStaffEmail).Code;

        factory.Clock.UtcNow = originalTime.AddMinutes(11); // past the 10-minute expiry
        try
        {
            var response = await client.PostAsync("/api/auth/verify-otp", new { loginToken, code });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            factory.Clock.UtcNow = originalTime;
        }
    }

    [Fact]
    public async Task GetSession_Unauthenticated_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ClearsCookie_SubsequentSessionCallReturns401()
    {
        var client = CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ActiveStaffEmail, password = CustomWebApplicationFactory.KnownPassword });
        var loginToken = (await ReadJsonAsync(loginResponse)).GetProperty("loginToken").GetString();
        var code = factory.OtpEmailSender.SentCodes.Last(c => c.Email == CustomWebApplicationFactory.ActiveStaffEmail).Code;
        await client.PostAsync("/api/auth/verify-otp", new { loginToken, code });

        var logoutResponse = await client.PostAsync("/api/auth/logout");
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var sessionResponse = await client.GetAsync("/api/auth/session");
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResendOtp_IssuesNewCodeAgainstSameToken()
    {
        var client = CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/login", new { email = CustomWebApplicationFactory.ActiveStaffEmail, password = CustomWebApplicationFactory.KnownPassword });
        var loginToken = (await ReadJsonAsync(loginResponse)).GetProperty("loginToken").GetString();
        var firstCode = factory.OtpEmailSender.SentCodes.Last(c => c.Email == CustomWebApplicationFactory.ActiveStaffEmail).Code;

        var resendResponse = await client.PostAsync("/api/auth/resend-otp", new { loginToken });
        resendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newCode = factory.OtpEmailSender.SentCodes.Last(c => c.Email == CustomWebApplicationFactory.ActiveStaffEmail).Code;

        newCode.Should().NotBe(firstCode);

        var verifyResponse = await client.PostAsync("/api/auth/verify-otp", new { loginToken, code = newCode });
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyOtp_MalformedCode_Returns400WithValidationErrorEnvelope()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/auth/verify-otp", new { loginToken = "some-token", code = "abc" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ReadJsonAsync(response);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }
}
