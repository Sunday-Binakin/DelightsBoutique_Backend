using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

namespace DelightsBoutique.Api.Middleware;

/// <summary>
/// Rejects a request whose declared Content-Length already exceeds its endpoint's own
/// [RequestSizeLimit], before model binding runs. Once IFormFile binding hits the limit,
/// the host turns Kestrel's BadHttpRequestException into a raw ProblemDetails body that no
/// application code can intercept — checking Content-Length up front means the standard
/// error envelope is returned instead. Requests without a declared Content-Length (chunked)
/// still fall through to Kestrel's own limit as before.
/// </summary>
public class UploadSizeGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var limit = context.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize;
        var declared = context.Request.ContentLength;

        if (limit is not null && declared is not null && declared > limit)
        {
            // Responding without reading the body makes Kestrel reset the connection, so the
            // client never sees this envelope (browsers report a network error instead). Drain
            // it first, lifting the per-request cap that would otherwise throw while reading.
            var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (sizeFeature is not null)
                sizeFeature.MaxRequestBodySize = null;
            await context.Request.Body.CopyToAsync(Stream.Null);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "VALIDATION_ERROR", message = "One or more fields are invalid.", fields = new { file = "File is too large." } }
            });
            return;
        }

        await next(context);
    }
}
