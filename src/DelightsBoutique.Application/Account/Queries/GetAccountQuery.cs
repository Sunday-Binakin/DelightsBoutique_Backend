using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Account.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Account.Queries;

public record GetAccountQuery : IRequest<AccountDto>;

public class GetAccountQueryHandler(ICurrentUser currentUser, IOwnerProfileRepository owners, IStaffRepository staff)
    : IRequestHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountQuery request, CancellationToken ct)
    {
        if (currentUser.Role == "Owner")
        {
            var owner = await owners.GetByEmailAsync(currentUser.Email!, ct) ?? throw new InvalidOperationException("Authenticated Owner has no OwnerProfile row.");
            return new AccountDto(owner.Name, owner.Email, owner.Phone, "Owner");
        }

        var member = await staff.GetByIdAsync(currentUser.StaffId!.Value, ct) ?? throw new InvalidOperationException("Authenticated Staff has no StaffMember row.");
        return new AccountDto(member.Name, member.Email, member.Phone, "Staff");
    }
}
