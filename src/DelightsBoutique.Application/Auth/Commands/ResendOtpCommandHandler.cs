using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Auth.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Auth.Commands;

public class ResendOtpCommandHandler(
    IOtpChallengeStore otpChallengeStore,
    IOtpEmailSender otpEmailSender,
    IOwnerProfileRepository ownerRepository,
    IStaffRepository staffRepository) : IRequestHandler<ResendOtpCommand, ResendOtpResult>
{
    public async Task<ResendOtpResult> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var resent = await otpChallengeStore.ResendAsync(request.LoginToken, cancellationToken);
        if (resent is null) return new ResendOtpFailed();

        var (code, userId, role) = resent.Value;

        if (role == "Owner")
        {
            var owner = await ownerRepository.GetByIdAsync(userId, cancellationToken);
            if (owner is null) return new ResendOtpFailed();
            await otpEmailSender.SendOtpAsync(owner.Email, owner.Name, code, cancellationToken);
        }
        else
        {
            var staff = await staffRepository.GetByIdAsync(userId, cancellationToken);
            if (staff is null) return new ResendOtpFailed();
            await otpEmailSender.SendOtpAsync(staff.Email, staff.Name, code, cancellationToken);
        }

        return new ResendOtpSucceeded();
    }
}
