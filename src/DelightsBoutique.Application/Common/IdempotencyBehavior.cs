using System.Text.Json;
using DelightsBoutique.Application.Abstractions;
using MediatR;

namespace DelightsBoutique.Application.Common;

/// <summary>
/// Registered OUTERMOST (before ValidationBehavior) in Program.cs — same key + different
/// body must 409 regardless of whether the new body would even pass validation; same key
/// + same body must replay the cached response without re-running validation or the
/// handler at all.
///
/// Wraps the handler's write + the idempotency-record write in a real transaction via
/// IUnitOfWork (unlike AuditLoggingBehavior's "separate SaveChangesAsync" precedent) —
/// BUILD_SPEC §8 explicitly calls this mechanism atomic, a stronger statement than it ever
/// makes about audit logging.
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse>(IIdempotencyStore store, IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand idempotent)
            return await next(); // not an idempotent command — pure pass-through, zero overhead

        var endpoint = typeof(TRequest).Name; // the (key, endpoint) discriminator
        var requestHash = RequestHasher.Hash(request);

        var existing = await store.FindAsync(idempotent.IdempotencyKey, endpoint, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
                throw new IdempotencyConflictException(idempotent.IdempotencyKey);

            // Same key, same body: replay verbatim. Validation and the handler never run.
            return JsonSerializer.Deserialize<TResponse>(existing.ResponseBody, JsonOptions)!;
        }

        TResponse response = default!;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            response = await next(); // runs ValidationBehavior -> AuditLoggingBehavior -> the handler; a throw here skips SaveAsync entirely, nothing is ever cached for a failed attempt
            var responseBody = JsonSerializer.Serialize(response, JsonOptions);
            await store.SaveAsync(idempotent.IdempotencyKey, endpoint, requestHash, 200, responseBody, innerCt);
        }, cancellationToken);

        return response;
    }
}
