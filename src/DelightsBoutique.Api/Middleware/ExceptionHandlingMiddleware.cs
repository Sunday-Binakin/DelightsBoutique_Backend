using DelightsBoutique.Application.Common;
using DelightsBoutique.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Api.Middleware;

/// <summary>
/// Grows as more domain exceptions are added in later phases. Validation was needed from
/// Phase 1's own live endpoints; CategoryInUseException/DuplicateSlugException are Phase 2's
/// first real callers of those exception types; ProductUnavailableException (existed since
/// Phase 1, unused until now), PaymentProofNotAllowedException, and IdempotencyConflictException
/// are Phase 3's. InvalidStatusTransitionException (existed since Phase 1, unused until now —
/// its first caller is the admin order-status PATCH) and DbUpdateConcurrencyException (Order's
/// Postgres xmin concurrency token firing on a stale write) are Phase 4's.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            // FluentValidation's default PropertyName is the raw C# property (PascalCase,
            // e.g. "Email") unless a validator hand-constructs a ValidationFailure with its
            // own lowercase name (several do, elsewhere in this codebase). Dictionary keys
            // are never touched by the JSON serializer's camelCase policy — that only
            // applies to reflected property names — so without this, "fields" came back
            // inconsistently cased depending on which validator produced the error. Fixed
            // centrally here, once, rather than hand-naming every RuleFor(...) call.
            var fields = ex.Errors.GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.First().ErrorMessage);
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "VALIDATION_ERROR", message = "One or more fields are invalid.", fields }
            });
        }
        catch (CategoryInUseException ex)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "CONFLICT", message = ex.Message } });
        }
        catch (DuplicateSlugException ex)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "CONFLICT", message = ex.Message } });
        }
        catch (ProductUnavailableException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "VALIDATION_ERROR", message = ex.Message } });
        }
        catch (PaymentProofNotAllowedException ex)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "CONFLICT", message = ex.Message } });
        }
        catch (IdempotencyConflictException ex)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "CONFLICT", message = ex.Message } });
        }
        catch (InvalidStatusTransitionException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "VALIDATION_ERROR", message = ex.Message } });
        }
        catch (DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "CONFLICT", message = "This record was changed by someone else. Please reload and try again." } });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new { code = "INTERNAL_ERROR", message = "An unexpected error occurred." }
            });
        }
    }

    // Per dot-separated segment, so a nested path like "Items[0].Slug" (from
    // RuleForEach/ChildRules) becomes "items[0].slug", not just its first letter touched.
    public static string ToCamelCase(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(segment =>
            segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));
}
