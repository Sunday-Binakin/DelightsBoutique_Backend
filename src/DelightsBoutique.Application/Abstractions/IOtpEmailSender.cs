namespace DelightsBoutique.Application.Abstractions;

/// <summary>
/// Intentionally minimal and separate from any future order/staff
/// <c>INotificationSender</c> (Phase 4) — OTP email is synchronous-and-blocking by
/// explicit requirement (the user is actively waiting on-screen for the code), a
/// different delivery contract than the async composite notification system. Do not
/// fold this into that interface later; they solve different problems.
/// </summary>
public interface IOtpEmailSender
{
    Task SendOtpAsync(string toEmail, string recipientName, string code, CancellationToken ct);
}
