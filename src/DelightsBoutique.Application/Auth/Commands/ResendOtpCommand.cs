using DelightsBoutique.Application.Auth.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Auth.Commands;

public record ResendOtpCommand(string LoginToken) : IRequest<ResendOtpResult>;
