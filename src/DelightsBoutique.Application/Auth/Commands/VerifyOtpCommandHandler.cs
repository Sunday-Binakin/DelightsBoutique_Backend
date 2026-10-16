using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Auth.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DelightsBoutique.Application.Auth.Commands;

public class VerifyOtpCommandHandler(
    IOtpChallengeStore otpChallengeStore,
    IOwnerProfileRepository ownerRepository,
    IStaffRepository staffRepository,
    ILogger<VerifyOtpCommandHandler> logger) : IRequestHandler<VerifyOtpCommand, VerifyOtpResult>
{
    public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var (outcome, success) = await otpChallengeStore.VerifyAsync(request.LoginToken, request.Code, cancellationToken);
        if (outcome != OtpVerifyOutcome.Succeeded || success is null)
            return new VerifyOtpFailed();

        if (success.Role == "Owner")
        {
            var owner = await ownerRepository.GetByIdAsync(success.UserId, cancellationToken);
            if (owner is null)
            {
                // The code was correct and is now burned — this leaves the user stuck until
                // they sign in again, so it's worth knowing exactly when it happens.
                logger.LogError("OTP verify succeeded but owner {UserId} no longer exists", success.UserId);
                return new VerifyOtpFailed();
            }

            // Owner bypasses every permission check (PermissionAuthorizationHandler succeeds
            // on the Owner claim before looking at any specific permission) — report the
            // full set here so the frontend can render the same admin UI it would for a
            // staff member who happens to hold every permission, rather than special-casing
            // "Owner" as a UI concept of its own.
            return new VerifyOtpSucceeded(owner.Id, "Owner", StaffId: null, owner.Email, owner.Name, Permissions: Enum.GetValues<Permission>());
        }

        var staff = await staffRepository.GetByIdAsync(success.UserId, cancellationToken);
        if (staff is null || !staff.Active)
        {
            logger.LogError("OTP verify succeeded but staff {UserId} is {Reason}", success.UserId, staff is null ? "missing" : "inactive");
            return new VerifyOtpFailed();
        }

        return new VerifyOtpSucceeded(staff.Id, "Staff", staff.Id, staff.Email, staff.Name, staff.Permissions);
    }
}
