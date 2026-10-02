using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Commands;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Staff;

public class ResetStaffPasswordCommandHandlerTests
{
    private readonly Mock<IStaffRepository> _staff = new();
    private readonly Mock<IPasswordGenerator> _passwordGenerator = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    private ResetStaffPasswordCommandHandler CreateHandler() => new(_staff.Object, _passwordGenerator.Object, _passwordHasher.Object);

    [Fact]
    public async Task Handle_ExistingStaff_ResetsPasswordAndReturnsItOnce()
    {
        var member = StaffMember.Create("Jane", "jane@x.com", null, [Permission.ManageOrders], "old-hash");
        _staff.Setup(s => s.GetByIdAsync(member.Id, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _passwordGenerator.Setup(g => g.Generate()).Returns("NewPass123!");
        _passwordHasher.Setup(h => h.Hash("NewPass123!")).Returns("new-hash");

        var result = await CreateHandler().Handle(new ResetStaffPasswordCommand(member.Id), CancellationToken.None);

        result!.Password.Should().Be("NewPass123!");
        member.PasswordHash.Should().Be("new-hash");
        _staff.Verify(s => s.UpdateAsync(member, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownStaffId_ReturnsNull()
    {
        _staff.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((StaffMember?)null);

        var result = await CreateHandler().Handle(new ResetStaffPasswordCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
