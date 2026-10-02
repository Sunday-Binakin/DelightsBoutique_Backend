using DelightsBoutique.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace DelightsBoutique.Infrastructure.Auth;

public class GmailSmtpOtpEmailSender(IConfiguration config) : IOtpEmailSender
{
    public async Task SendOtpAsync(string toEmail, string recipientName, string code, CancellationToken ct)
    {
        var fromEmail = config["Gmail:Email"] ?? throw new InvalidOperationException("Gmail:Email must be configured.");
        var appPassword = config["Gmail:AppPassword"] ?? throw new InvalidOperationException("Gmail:AppPassword must be configured.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your Delight's Boutique login code";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Hi {recipientName},</p><p>Your one-time login code is <strong>{code}</strong>. It expires in 10 minutes.</p>"
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(fromEmail, appPassword, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Login synchronously depends on this send succeeding — must fail loudly,
            // never silently return a loginToken with no email actually sent.
            throw new InvalidOperationException("Failed to send OTP email.", ex);
        }
    }
}
