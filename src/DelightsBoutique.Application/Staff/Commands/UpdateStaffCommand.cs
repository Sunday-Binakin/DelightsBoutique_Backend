using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Staff.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Staff.Commands;

public record UpdateStaffCommand(Guid Id, string? Name, string? Email, string? Phone, List<Permission>? Permissions, bool? Active)
    : IRequest<StaffDto?>, IAuditableCommand
{
    public string DescribeAction() => $"Updated staff member '{Id}'";
}
