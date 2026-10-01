using DelightsBoutique.Application.Auth.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;
