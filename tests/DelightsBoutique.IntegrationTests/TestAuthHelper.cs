using System.Text.Json;

namespace DelightsBoutique.IntegrationTests;

public static class TestAuthHelper
{
    public static async Task<AuthTestClient> LoginAsAsync(CustomWebApplicationFactory factory, string email, string password = CustomWebApplicationFactory.KnownPassword)
    {
        var client = new AuthTestClient(factory.CreateClient());

        var loginResponse = await client.PostAsync("/api/auth/login", new { email, password });
        var loginBody = await ReadJsonAsync(loginResponse);
        var loginToken = loginBody.GetProperty("loginToken").GetString();

        var code = factory.OtpEmailSender.SentCodes.Last(c => c.Email == email).Code;
        await client.PostAsync("/api/auth/verify-otp", new { loginToken, code });

        return client;
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
}
