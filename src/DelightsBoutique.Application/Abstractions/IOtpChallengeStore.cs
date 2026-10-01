namespace DelightsBoutique.Application.Abstractions;

public enum OtpVerifyOutcome
{
    Succeeded,
    InvalidOrExpired
}

public record OtpVerifySuccess(Guid UserId, string Role);

public interface IOtpChallengeStore
{
    /// <summary>Generates a code, persists its hash, and returns a new opaque login token plus the plaintext code (for the caller to email — never returned to the client).</summary>
    Task<(string LoginToken, string Code)> CreateAsync(Guid userId, string role, CancellationToken ct);

    Task<(OtpVerifyOutcome Outcome, OtpVerifySuccess? Success)> VerifyAsync(string loginToken, string code, CancellationToken ct);

    /// <summary>Re-generates a code against the same token row. Returns the new plaintext code plus who it belongs to (so the caller can email it), or null if the token is unknown/already consumed.</summary>
    Task<(string Code, Guid UserId, string Role)?> ResendAsync(string loginToken, CancellationToken ct);
}
