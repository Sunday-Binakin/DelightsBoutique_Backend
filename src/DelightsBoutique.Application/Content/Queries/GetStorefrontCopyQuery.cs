using System.Text.Json;
using MediatR;

namespace DelightsBoutique.Application.Content.Queries;

public record GetStorefrontCopyQuery : IRequest<JsonElement>;
