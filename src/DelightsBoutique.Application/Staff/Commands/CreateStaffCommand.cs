using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Staff.Dtos;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Staff.Commands;

public record CreateStaffCommand(string IdempotencyKey, string Name, string Email, string? Phone, List<Permission> Permissions)
    : IRequest<CreateStaffResult>, IIdempotentCommand, IAuditableCommand
{
    public string DescribeAction() => $"Created staff member '{Email}'";
}
