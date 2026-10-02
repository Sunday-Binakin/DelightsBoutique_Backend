using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record UpdateCollectionSectionCommand(
    string? Eyebrow, string? Title, string? NewArrivalsLabel, string? BestSellersLabel, string? WholesalePicksLabel,
    List<string>? NewArrivalsSlugs, List<string>? BestSellersSlugs, List<string>? WholesalePicksSlugs)
    : IRequest<CollectionSectionDto>, IAuditableCommand
{
    public string DescribeAction() => "Updated collection section content";
}
