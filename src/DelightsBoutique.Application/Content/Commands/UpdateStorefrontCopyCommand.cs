using System.Text.Json;
using DelightsBoutique.Application.Common;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record UpdateStorefrontCopyCommand(JsonElement Patch) : IRequest<JsonElement>, IAuditableCommand
{
    public string DescribeAction() => "Updated storefront copy";
}
