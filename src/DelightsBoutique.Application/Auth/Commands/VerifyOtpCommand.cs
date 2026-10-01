using DelightsBoutique.Application.Auth.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Auth.Commands;

public record VerifyOtpCommand(string LoginToken, string Code) : IRequest<VerifyOtpResult>;
