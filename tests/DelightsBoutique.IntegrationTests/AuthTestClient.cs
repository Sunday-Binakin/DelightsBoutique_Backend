using System.Net.Http.Headers;

namespace DelightsBoutique.IntegrationTests;

/// <summary>
/// WebApplicationFactory.CreateClient() does NOT persist cookies between requests by
/// default (unlike a browser) — this wrapper extracts Set-Cookie from a response and
/// attaches it to subsequent requests. Without this, every test would mysteriously
/// 401 right after a successful login.
/// </summary>
public class AuthTestClient(HttpClient client)
{
    private string? _sessionCookie;

    public HttpClient Client => client;

    public async Task<HttpResponseMessage> PostAsync(string url, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
            request.Content = JsonContent(body);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        AttachCookie(request);

        var response = await client.SendAsync(request);
        CaptureCookie(response);
        return response;
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AttachCookie(request);

        var response = await client.SendAsync(request);
        CaptureCookie(response);
        return response;
    }

    public Task<HttpResponseMessage> PatchAsync(string url, object? body = null) => SendAsync(HttpMethod.Patch, url, body);
    public Task<HttpResponseMessage> PutAsync(string url, object? body = null) => SendAsync(HttpMethod.Put, url, body);
    public Task<HttpResponseMessage> DeleteAsync(string url) => SendAsync(HttpMethod.Delete, url, null);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent(body);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        AttachCookie(request);

        var response = await client.SendAsync(request);
        CaptureCookie(response);
        return response;
    }

    public void ClearSession() => _sessionCookie = null;

    private void AttachCookie(HttpRequestMessage request)
    {
        if (_sessionCookie is not null)
            request.Headers.Add("Cookie", _sessionCookie);
    }

    private void CaptureCookie(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var cookie = cookies.FirstOrDefault();
            if (cookie is not null)
            {
                // Strip attributes (path, expires, etc.) — only the name=value pair is needed on the request.
                _sessionCookie = cookie.Split(';')[0];
            }
        }
    }

    private static StringContent JsonContent(object body)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
