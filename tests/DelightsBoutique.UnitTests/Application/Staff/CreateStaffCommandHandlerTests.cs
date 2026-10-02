using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Commands;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Staff;

public class CreateStaffCommandHandlerTests
{
    private readonly Mock<IStaffRepository> _staff = new();
    private readonly Mock<IPasswordGenerator> _passwordGenerator = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<INotificationQueue> _notifications = new();

    private CreateStaffCommandHandler CreateHandler() => new(_staff.Object, _passwordGenerator.Object, _passwordHasher.Object, _notifications.Object);

    [Fact]
    public async Task Handle_HappyPath_CreatesStaffAndReturnsOneTimePassword()
    {
        _passwordGenerator.Setup(g => g.Generate()).Returns("TempPass123!");
        _passwordHasher.Setup(h => h.Hash("TempPass123!")).Returns("hashed-password");

        var result = await CreateHandler().Handle(
            new CreateStaffCommand("idem-key", "Jane Staff", "Jane@Example.com", "0241234567", [Permission.ManageOrders]),
            CancellationToken.None);

        result.Password.Should().Be("TempPass123!");
        result.Member.Email.Should().Be("jane@example.com"); // lowercased for storage
        result.Member.Name.Should().Be("Jane Staff");
        _staff.Verify(s => s.AddAsync(It.Is<StaffMember>(m => m.PasswordHash == "hashed-password"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EnqueuesEmailOnlyNotification_NoWhatsApp()
    {
        _passwordGenerator.Setup(g => g.Generate()).Returns("TempPass123!");
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

        await CreateHandler().Handle(new CreateStaffCommand("idem-key", "Jane", "jane@x.com", null, [Permission.ManageOrders]), CancellationToken.None);

        _notifications.Verify(n => n.Enqueue(It.Is<NotificationMessage>(m =>
            m.RecipientEmail == "jane@x.com" && m.RecipientPhone == null)), Times.Once);
    }
}
