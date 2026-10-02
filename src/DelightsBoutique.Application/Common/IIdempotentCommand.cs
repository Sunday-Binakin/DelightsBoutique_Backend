namespace DelightsBoutique.Application.Common;

/// <summary>
/// Marker + carrier for the client-supplied Idempotency-Key header. The controller reads
/// the header and attaches it to the command before calling mediator.Send — Application
/// never touches HttpContext directly (same rule that put ICurrentUser's implementation
/// in Api, not Infrastructure). Picked up by IdempotencyBehavior.
/// </summary>
public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
}
