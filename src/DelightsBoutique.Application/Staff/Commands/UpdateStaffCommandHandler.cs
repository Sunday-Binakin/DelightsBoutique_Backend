using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Dtos;
using DelightsBoutique.Application.Staff.Queries;
using MediatR;

namespace DelightsBoutique.Application.Staff.Commands;

public class UpdateStaffCommandHandler(IStaffRepository staff) : IRequestHandler<UpdateStaffCommand, StaffDto?>
{
    public async Task<StaffDto?> Handle(UpdateStaffCommand request, CancellationToken ct)
    {
        var member = await staff.GetByIdAsync(request.Id, ct);
        if (member is null) return null;

        member.UpdateProfile(request.Name, request.Email?.ToLowerInvariant(), request.Phone, request.Permissions, request.Active);
        await staff.UpdateAsync(member, ct);

        return StaffDtoMapper.ToDto(member);
    }
}
