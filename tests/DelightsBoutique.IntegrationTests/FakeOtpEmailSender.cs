using System.Collections.Concurrent;
using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.IntegrationTests;

/// <summary>
/// Swapped in for the real Resend-backed sender in tests — the real OTP code only
/// ever exists hashed in the DB, so this is the only way to retrieve a working code
/// without calling the real Resend API from a test run.
/// </summary>
public class FakeOtpEmailSender : IOtpEmailSender
{
    public ConcurrentBag<(string Email, string Code)> SentCodes { get; } = [];

    public Task SendOtpAsync(string toEmail, string recipientName, string code, CancellationToken ct)
    {
        SentCodes.Add((toEmail, code));
        return Task.CompletedTask;
    }
}
