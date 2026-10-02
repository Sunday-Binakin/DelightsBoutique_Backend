using DelightsBoutique.Application.Common;
using MediatR;

namespace DelightsBoutique.Application.Categories.Commands;

public record DeleteCategoryCommand(string Slug) : IRequest<bool>, IAuditableCommand
{
    public string DescribeAction() => $"Deleted category '{Slug}'";
}
