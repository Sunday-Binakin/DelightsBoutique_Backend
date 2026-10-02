using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DelightsBoutique.UnitTests.Infrastructure;

public class CompositeNotificationSenderTests
{
    private static NotificationMessage SampleMessage() => new("to@example.com", "0241234567", "Subject", "Body");

    [Fact]
    public async Task SendAsync_CallsEveryRegisteredChannel()
    {
        var channel1 = new Mock<INotificationChannel>();
        var channel2 = new Mock<INotificationChannel>();
        var sender = new CompositeNotificationSender([channel1.Object, channel2.Object], Mock.Of<ILogger<CompositeNotificationSender>>());

        var message = SampleMessage();
        await sender.SendAsync(message, CancellationToken.None);

        channel1.Verify(c => c.SendAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        channel2.Verify(c => c.SendAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_OneChannelThrows_OtherChannelStillCalled()
    {
        var failingChannel = new Mock<INotificationChannel>();
        failingChannel.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("provider down"));
        var healthyChannel = new Mock<INotificationChannel>();
        var sender = new CompositeNotificationSender([failingChannel.Object, healthyChannel.Object], Mock.Of<ILogger<CompositeNotificationSender>>());

        var act = () => sender.SendAsync(SampleMessage(), CancellationToken.None);

        await act.Should().NotThrowAsync(); // the composite itself never propagates a channel's failure
        healthyChannel.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_NoChannelsRegistered_DoesNotThrow()
    {
        var sender = new CompositeNotificationSender([], Mock.Of<ILogger<CompositeNotificationSender>>());

        var act = () => sender.SendAsync(SampleMessage(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
