using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public record GetFlashSaleQuery : IRequest<FlashSaleDto>;
