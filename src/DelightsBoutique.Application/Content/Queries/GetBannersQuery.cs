using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public record GetBannersQuery : IRequest<BannerSetDto>;
