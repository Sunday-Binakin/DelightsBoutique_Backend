using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record UpdateThemeCommand(string? Primary, string? Accent) : IRequest<ThemeDto>, IAuditableCommand
{
    public string DescribeAction() => "Updated theme colors";
}
