using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Staff.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Staff.Commands;

public record ResetStaffPasswordCommand(Guid Id) : IRequest<ResetPasswordResult?>, IAuditableCommand
{
    public string DescribeAction() => $"Reset password for staff member '{Id}'";
}
