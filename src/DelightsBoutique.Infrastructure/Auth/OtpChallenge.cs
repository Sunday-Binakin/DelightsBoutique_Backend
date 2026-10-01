namespace DelightsBoutique.Infrastructure.Auth;

/// <summary>
/// Infrastructure-only — the OTP login flow extends BUILD_SPEC's original API contract
/// (per §11 item 4), and this entity has no business meaning outside auth plumbing.
/// </summary>
public class OtpChallenge
{
    public Guid Id { get; set; }
    public string LoginToken { get; set; } = default!;
    public Guid UserId { get; set; }
    public string Role { get; set; } = default!; // "Owner" | "Staff"
    public string HashedCode { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
