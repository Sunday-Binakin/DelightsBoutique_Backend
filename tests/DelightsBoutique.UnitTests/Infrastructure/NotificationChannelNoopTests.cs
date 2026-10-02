using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DelightsBoutique.UnitTests.Infrastructure;

public class NotificationChannelNoopTests
{
    [Fact]
    public async Task EmailNotificationSender_NullRecipientEmail_NoOpsWithoutTouchingConfig()
    {
        // No Gmail config is set up at all — if this tried to actually send, it would throw
        // on the missing config before ever reaching the network, proving the null-recipient
        // check short-circuits first.
        var sender = new EmailNotificationSender(Mock.Of<IConfiguration>(), Mock.Of<ILogger<EmailNotificationSender>>());

        var act = () => sender.SendAsync(new NotificationMessage(null, "0241234567", "Subject", "Body"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NoopWhatsAppNotificationSender_NeverThrows_RegardlessOfRecipient()
    {
        var sender = new NoopWhatsAppNotificationSender(Mock.Of<ILogger<NoopWhatsAppNotificationSender>>());

        var act = () => sender.SendAsync(new NotificationMessage("to@example.com", "0241234567", "Subject", "Body"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
