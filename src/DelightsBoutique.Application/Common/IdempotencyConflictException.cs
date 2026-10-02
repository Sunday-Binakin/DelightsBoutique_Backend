namespace DelightsBoutique.Application.Common;

/// <summary>
/// Same reasoning as IdempotencyKey itself: idempotency plumbing has no business meaning
/// outside this mechanism, so this does not derive from DomainException and does not live
/// in Domain. Mapped to 409 CONFLICT in ExceptionHandlingMiddleware.
/// </summary>
public class IdempotencyConflictException(string key) : Exception($"Idempotency key '{key}' was reused with a different request body.")
{
    public string Key { get; } = key;
}
