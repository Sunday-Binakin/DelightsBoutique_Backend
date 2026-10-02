using DelightsBoutique.Application.Common;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record DeleteFaqCommand(string Id) : IRequest<bool>, IAuditableCommand
{
    public string DescribeAction() => $"Deleted FAQ '{Id}'";
}
