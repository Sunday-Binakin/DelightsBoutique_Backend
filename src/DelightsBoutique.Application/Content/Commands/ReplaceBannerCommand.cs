using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record ReplaceBannerCommand(string Key, BannerDto Banner) : IRequest<BannerSetDto?>, IAuditableCommand
{
    public string DescribeAction() => $"Updated homepage banner '{Key}'";
}
