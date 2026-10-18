using System.Net;
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
        message.From.Add(new MailboxAddress("Delight's Boutique", fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your Delight's Boutique login code";
        message.Body = new TextPart("html")
        {
            Text = BuildHtmlBody(recipientName, code)
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

    private static string BuildHtmlBody(string recipientName, string code)
    {
        var name = WebUtility.HtmlEncode(recipientName);
        var safeCode = WebUtility.HtmlEncode(code);
        var spacedCode = string.Join(" ", safeCode.ToCharArray());

        return $$"""
        <!doctype html>
        <html>
          <body style="margin:0; padding:0; background-color:#1a2a7a; font-family:Helvetica, Arial, sans-serif;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#1a2a7a; background-image:linear-gradient(160deg, #15216e 0%, #3a4bb0 35%, #a34fc7 60%, #ff8a2b 100%); padding:40px 16px;">
              <tr>
                <td align="center">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:420px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(0,0,0,0.25);">
                    <tr>
                      <td style="background-color:#1b2b8a; background-image:linear-gradient(135deg, #15206b 0%, #2c45c2 100%); padding:32px 32px 28px 32px; text-align:center;">
                        <div style="font-family:Georgia, 'Times New Roman', serif; font-size:26px; font-weight:bold; color:#ffffff;">Delight&rsquo;s</div>
                        <div style="font-family:Georgia, 'Times New Roman', serif; font-size:22px; font-style:italic; color:#ffffff; margin-top:-4px;">Boutique</div>
                        <div style="font-family:Georgia, 'Times New Roman', serif; font-size:12px; font-style:italic; color:#bcd1ff; margin-top:10px;">Whatever your class&hellip; whatever your style&hellip;</div>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:32px 32px 8px 32px;">
                        <p style="margin:0 0 16px 0; font-size:15px; line-height:1.5; color:#2b2b2b;">Hi {{name}},</p>
                        <p style="margin:0 0 24px 0; font-size:15px; line-height:1.5; color:#2b2b2b;">Use the code below to finish signing in. It&rsquo;s only valid for the next 10 minutes.</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:0 32px;">
                        <div style="background-color:#fff4ea; border:1px solid #ffcf9e; border-radius:12px; padding:20px; text-align:center;">
                          <span style="font-family:'Courier New', monospace; font-size:32px; font-weight:bold; letter-spacing:8px; color:#ff7a1a;">{{spacedCode}}</span>
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px 32px 32px 32px;">
                        <p style="margin:0; font-size:13px; line-height:1.5; color:#8a8a8a;">Didn&rsquo;t try to sign in? You can safely ignore this email &mdash; your account is still secure.</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:18px 32px; background-color:#15206b; text-align:center;">
                        <span style="font-size:11px; letter-spacing:0.5px; color:#bcd1ff;">Bags &middot; Dresses &middot; Shoes &middot; Jewelries &middot; Fabrics</span>
                        <br />
                        <span style="font-size:11px; color:#7f93dd;">&copy; {{DateTime.UtcNow.Year}} Delight&rsquo;s Boutique</span>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </body>
        </html>
        """;
    }
}
