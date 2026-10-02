using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record UpdateFaqCommand(string Id, string? Question, string? Answer) : IRequest<FaqItemDto?>, IAuditableCommand
{
    public string DescribeAction() => $"Updated FAQ '{Id}'";
}
