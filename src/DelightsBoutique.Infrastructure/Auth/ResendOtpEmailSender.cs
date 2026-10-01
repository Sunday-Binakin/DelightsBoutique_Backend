using DelightsBoutique.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Resend;

namespace DelightsBoutique.Infrastructure.Auth;

public class ResendOtpEmailSender(IResend resend, IConfiguration config) : IOtpEmailSender
{
    public async Task SendOtpAsync(string toEmail, string recipientName, string code, CancellationToken ct)
    {
        try
        {
            await resend.EmailSendAsync(new EmailMessage
            {
                From = config["Resend:FromAddress"] ?? "onboarding@resend.dev",
                To = toEmail,
                Subject = "Your Delight's Boutique login code",
                HtmlBody = $"<p>Hi {recipientName},</p><p>Your one-time login code is <strong>{code}</strong>. It expires in 10 minutes.</p>"
            }, ct);
        }
        catch (Exception ex)
        {
            // Login synchronously depends on this send succeeding — must fail loudly,
            // never silently return a loginToken with no email actually sent.
            throw new InvalidOperationException("Failed to send OTP email.", ex);
        }
    }
}
