using DelightsBoutique.Domain.Entities;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class OrderNotificationTests
{
    [Fact]
    public void Create_DefaultsReadToFalse()
    {
        var notification = OrderNotification.Create("DB-TEST1", "Jane Doe", 100m);

        notification.Read.Should().BeFalse();
    }

    [Fact]
    public void MarkRead_SetsReadTrue()
    {
        var notification = OrderNotification.Create("DB-TEST1", "Jane Doe", 100m);

        notification.MarkRead();

        notification.Read.Should().BeTrue();
    }
}
