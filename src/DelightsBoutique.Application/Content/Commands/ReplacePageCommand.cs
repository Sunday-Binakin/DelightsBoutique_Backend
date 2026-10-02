using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record ReplacePageCommand(string Key, string Title, string Body) : IRequest<PageContentDto?>, IAuditableCommand
{
    public string DescribeAction() => $"Updated page '{Key}'";
}
