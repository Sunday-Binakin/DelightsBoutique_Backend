using DelightsBoutique.Application.Common;
using DelightsBoutique.Domain.Exceptions;
using FluentValidation;

namespace DelightsBoutique.Api.Middleware;

/// <summary>
/// Grows as more domain exceptions are added in later phases. Validation was needed from
/// Phase 1's own live endpoints; CategoryInUseException/DuplicateSlugException are Phase 2's
/// first real callers of those exception types; ProductUnavailableException (existed since
/// Phase 1, unused until now), PaymentProofNotAllowedException, and IdempotencyConflictException
/// are Phase 3's. InvalidStatusTransitionException is deliberately NOT here yet — no caller
/// exists until Phase 4's admin order-status PATCH.
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
            var fields = ex.Errors.GroupBy(e => e.PropertyName)
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
}
