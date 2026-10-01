using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Auth.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Auth.Commands;

public class LoginCommandHandler(
    IOwnerProfileRepository ownerRepository,
    IStaffRepository staffRepository,
    IPasswordHasher passwordHasher,
    IOtpChallengeStore otpChallengeStore,
    IOtpEmailSender otpEmailSender) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Owner is tried first, then staff — matches the architecture doc's documented order.
        var owner = await ownerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (owner is not null)
        {
            if (!passwordHasher.Verify(request.Password, owner.PasswordHash))
                return new LoginFailed();

            var (loginToken, code) = await otpChallengeStore.CreateAsync(owner.Id, "Owner", cancellationToken);
            await otpEmailSender.SendOtpAsync(owner.Email, owner.Name, code, cancellationToken);
            return new LoginSucceeded(loginToken);
        }

        var staff = await staffRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (staff is null || !passwordHasher.Verify(request.Password, staff.PasswordHash))
        {
            // Unknown email and wrong password are deliberately indistinguishable —
            // otherwise the response itself leaks which emails have accounts.
            return new LoginFailed();
        }

        if (!staff.Active)
            return new LoginStaffDeactivated();

        var (staffLoginToken, staffCode) = await otpChallengeStore.CreateAsync(staff.Id, "Staff", cancellationToken);
        await otpEmailSender.SendOtpAsync(staff.Email, staff.Name, staffCode, cancellationToken);
        return new LoginSucceeded(staffLoginToken);
    }
}
