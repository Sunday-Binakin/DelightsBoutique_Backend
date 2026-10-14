using DelightsBoutique.Application.Abstractions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DelightsBoutique.Application.Account.Commands;

// IRequest<Unit>, not bare IRequest — MediatR 14's non-generic IRequest/IRequestHandler<TRequest>
// adapter form was confirmed (empirically, against the real pipeline) to bypass registered
// IPipelineBehavior<,> instances entirely, silently skipping FluentValidation. Every other
// command in this codebase already returns an explicit typed response for exactly this
// reason — this just follows that same proven-working shape instead of the adapter.
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Unit>;

public class ChangePasswordCommandHandler(
    ICurrentUser currentUser, IOwnerProfileRepository owners, IStaffRepository staff, IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (currentUser.Role == "Owner")
        {
            var owner = await owners.GetByEmailAsync(currentUser.Email!, ct) ?? throw new InvalidOperationException("Authenticated Owner has no OwnerProfile row.");
            VerifyCurrentPassword(request.CurrentPassword, owner.PasswordHash);
            owner.ResetPassword(passwordHasher.Hash(request.NewPassword));
            await owners.UpdateAsync(owner, ct);
            return Unit.Value;
        }

        var member = await staff.GetByIdAsync(currentUser.StaffId!.Value, ct) ?? throw new InvalidOperationException("Authenticated Staff has no StaffMember row.");
        VerifyCurrentPassword(request.CurrentPassword, member.PasswordHash);
        member.ResetPassword(passwordHasher.Hash(request.NewPassword));
        await staff.UpdateAsync(member, ct);
        return Unit.Value;
    }

    private void VerifyCurrentPassword(string currentPassword, string hash)
    {
        if (!passwordHasher.Verify(currentPassword, hash))
            throw new ValidationException([new ValidationFailure("currentPassword", "Current password is incorrect.")]);
    }
}
