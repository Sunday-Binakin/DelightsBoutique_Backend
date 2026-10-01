using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Auth.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Auth.Commands;

public class VerifyOtpCommandHandler(
    IOtpChallengeStore otpChallengeStore,
    IOwnerProfileRepository ownerRepository,
    IStaffRepository staffRepository) : IRequestHandler<VerifyOtpCommand, VerifyOtpResult>
{
    public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var (outcome, success) = await otpChallengeStore.VerifyAsync(request.LoginToken, request.Code, cancellationToken);
        if (outcome != OtpVerifyOutcome.Succeeded || success is null)
            return new VerifyOtpFailed();

        if (success.Role == "Owner")
        {
            var owner = await ownerRepository.GetByIdAsync(success.UserId, cancellationToken);
            if (owner is null) return new VerifyOtpFailed();

            return new VerifyOtpSucceeded(owner.Id, "Owner", StaffId: null, owner.Email, owner.Name, Permissions: []);
        }

        var staff = await staffRepository.GetByIdAsync(success.UserId, cancellationToken);
        if (staff is null || !staff.Active) return new VerifyOtpFailed();

        return new VerifyOtpSucceeded(staff.Id, "Staff", staff.Id, staff.Email, staff.Name, staff.Permissions);
    }
}
