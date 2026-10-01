using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Auth.Commands;
using DelightsBoutique.Application.Auth.Dtos;
using DelightsBoutique.Domain.Entities;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IOwnerProfileRepository> _ownerRepository = new();
    private readonly Mock<IStaffRepository> _staffRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IOtpChallengeStore> _otpChallengeStore = new();
    private readonly Mock<IOtpEmailSender> _otpEmailSender = new();

    private LoginCommandHandler CreateHandler() => new(
        _ownerRepository.Object, _staffRepository.Object, _passwordHasher.Object,
        _otpChallengeStore.Object, _otpEmailSender.Object);

    [Fact]
    public async Task Handle_CorrectOwnerCredentials_SucceedsAndSendsOtp()
    {
        var owner = OwnerProfile.Create("Store Owner", "owner@example.com", null, "hashed");
        _ownerRepository.Setup(r => r.GetByEmailAsync("owner@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed")).Returns(true);
        _otpChallengeStore.Setup(s => s.CreateAsync(owner.Id, "Owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("login-token", "123456"));

        var result = await CreateHandler().Handle(new LoginCommand("owner@example.com", "correct-password"), CancellationToken.None);

        result.Should().BeOfType<LoginSucceeded>().Which.LoginToken.Should().Be("login-token");
        _otpEmailSender.Verify(s => s.SendOtpAsync(owner.Email, owner.Name, "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CorrectActiveStaffCredentials_Succeeds()
    {
        var staff = StaffMember.ForTesting("staff@example.com", "hashed", active: true);
        _ownerRepository.Setup(r => r.GetByEmailAsync("staff@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((OwnerProfile?)null);
        _staffRepository.Setup(r => r.GetByEmailAsync("staff@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed")).Returns(true);
        _otpChallengeStore.Setup(s => s.CreateAsync(staff.Id, "Staff", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("login-token", "654321"));

        var result = await CreateHandler().Handle(new LoginCommand("staff@example.com", "correct-password"), CancellationToken.None);

        result.Should().BeOfType<LoginSucceeded>().Which.LoginToken.Should().Be("login-token");
    }

    [Fact]
    public async Task Handle_DeactivatedStaff_ReturnsLoginStaffDeactivated()
    {
        var staff = StaffMember.ForTesting("staff@example.com", "hashed", active: false);
        _ownerRepository.Setup(r => r.GetByEmailAsync("staff@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((OwnerProfile?)null);
        _staffRepository.Setup(r => r.GetByEmailAsync("staff@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed")).Returns(true);

        var result = await CreateHandler().Handle(new LoginCommand("staff@example.com", "correct-password"), CancellationToken.None);

        result.Should().BeOfType<LoginStaffDeactivated>();
        _otpChallengeStore.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsLoginFailed()
    {
        var owner = OwnerProfile.Create("Store Owner", "owner@example.com", null, "hashed");
        _ownerRepository.Setup(r => r.GetByEmailAsync("owner@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _passwordHasher.Setup(h => h.Verify("wrong-password", "hashed")).Returns(false);

        var result = await CreateHandler().Handle(new LoginCommand("owner@example.com", "wrong-password"), CancellationToken.None);

        result.Should().BeOfType<LoginFailed>();
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsLoginFailed_SameAsWrongPassword()
    {
        _ownerRepository.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((OwnerProfile?)null);
        _staffRepository.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((StaffMember?)null);

        var result = await CreateHandler().Handle(new LoginCommand("nobody@example.com", "whatever"), CancellationToken.None);

        // Unknown email and wrong password must be indistinguishable to the caller.
        result.Should().BeOfType<LoginFailed>();
    }
}
