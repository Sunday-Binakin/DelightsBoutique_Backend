namespace DelightsBoutique.Api.Middleware;

/// <summary>Requires a custom header on mutating requests, per BUILD_SPEC §6.1's CSRF line.</summary>
public class CsrfHeaderMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> MutatingMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (MutatingMethods.Contains(context.Request.Method) && !context.Request.Headers.ContainsKey("X-Requested-With"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "VALIDATION_ERROR", message = "X-Requested-With header is required." }
            });
            return;
        }

        await next(context);
    }
}
