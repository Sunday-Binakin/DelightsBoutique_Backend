namespace DelightsBoutique.Application.Common;

/// <summary>
/// Injectable clock so time-dependent logic (OTP expiry, idempotency-key cleanup
/// windows) can be tested deterministically without real waiting.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
