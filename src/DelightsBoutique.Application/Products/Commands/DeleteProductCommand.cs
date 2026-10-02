using DelightsBoutique.Application.Common;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

public record DeleteProductCommand(string Slug) : IRequest<bool>, IAuditableCommand
{
    public string DescribeAction() => $"Deleted product '{Slug}'";
}
