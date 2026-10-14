using MediatR;

namespace DelightsBoutique.Application.Uploads.Commands;

public record UploadImageCommand(byte[] FileBytes) : IRequest<UploadImageResult>;

public record UploadImageResult(string Url, int Width, int Height);
