using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record UpdateFlashSaleCommand(string? Eyebrow, string? Title, DateTime? EndsAt, List<string>? ProductSlugs)
    : IRequest<FlashSaleDto>, IAuditableCommand
{
    public string DescribeAction() => "Updated flash sale content";
}
