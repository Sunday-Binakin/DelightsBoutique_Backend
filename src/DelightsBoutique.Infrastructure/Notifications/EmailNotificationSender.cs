using DelightsBoutique.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DelightsBoutique.Infrastructure.Notifications;

/// <summary>Reuses the same Gmail SMTP credentials as GmailSmtpOtpEmailSender — same
/// provider, different call site (async-queued notifications, not synchronous OTP).</summary>
public class EmailNotificationSender(IConfiguration config, ILogger<EmailNotificationSender> logger) : INotificationChannel
{
    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail))
            return; // channel disabled for this message

        var fromEmail = config["Gmail:Email"] ?? throw new InvalidOperationException("Gmail:Email must be configured.");
        var appPassword = config["Gmail:AppPassword"] ?? throw new InvalidOperationException("Gmail:AppPassword must be configured.");

        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(fromEmail));
        mime.To.Add(MailboxAddress.Parse(message.RecipientEmail));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("html") { Text = message.Body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(fromEmail, appPassword, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Runs off the background queue, not inline with a customer request — log and
            // move on rather than throw, so one failed notification can't take down the
            // processor loop (CompositeNotificationSender also catches per-channel, this is
            // belt-and-suspenders for direct callers/tests of this class).
            logger.LogError(ex, "Failed to send email notification to {Email}", message.RecipientEmail);
            throw;
        }
    }
}
