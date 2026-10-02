using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Staff.Dtos;
using DelightsBoutique.Domain.Entities;
using MediatR;

namespace DelightsBoutique.Application.Staff.Queries;

public record GetStaffQuery : IRequest<List<StaffDto>>;

public class GetStaffQueryHandler(IStaffRepository staff) : IRequestHandler<GetStaffQuery, List<StaffDto>>
{
    public async Task<List<StaffDto>> Handle(GetStaffQuery request, CancellationToken ct)
    {
        var members = await staff.GetAllAsync(ct);
        return members.Select(StaffDtoMapper.ToDto).ToList();
    }
}

internal static class StaffDtoMapper
{
    public static StaffDto ToDto(StaffMember s) =>
        new(s.Id, s.Name, s.Email, s.Phone, s.Permissions, s.Active, s.CreatedAt, s.UpdatedAt);
}
