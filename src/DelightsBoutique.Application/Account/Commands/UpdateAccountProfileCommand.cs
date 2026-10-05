using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Account.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Account.Commands;

public record UpdateAccountProfileCommand(string? Name, string? Phone) : IRequest<AccountDto>;

/// <summary>
/// Self-service only: resolves "which account" from ICurrentUser (the authenticated
/// session), never from a request body id — unlike AdminStaffController's UpdateStaff,
/// which takes an explicit id and is gated by ManageStaff. This handler can only ever
/// touch the caller's own account.
/// </summary>
public class UpdateAccountProfileCommandHandler(
    ICurrentUser currentUser, IOwnerProfileRepository owners, IStaffRepository staff)
    : IRequestHandler<UpdateAccountProfileCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountProfileCommand request, CancellationToken ct)
    {
        if (currentUser.Role == "Owner")
        {
            var owner = await owners.GetByEmailAsync(currentUser.Email!, ct) ?? throw new InvalidOperationException("Authenticated Owner has no OwnerProfile row.");
            owner.UpdateProfile(request.Name, request.Phone);
            await owners.UpdateAsync(owner, ct);
            return new AccountDto(owner.Name, owner.Email, owner.Phone, "Owner");
        }

        var member = await staff.GetByIdAsync(currentUser.StaffId!.Value, ct) ?? throw new InvalidOperationException("Authenticated Staff has no StaffMember row.");
        member.UpdateProfile(request.Name, email: null, phone: request.Phone, permissions: null, active: null);
        await staff.UpdateAsync(member, ct);
        return new AccountDto(member.Name, member.Email, member.Phone, "Staff");
    }
}
