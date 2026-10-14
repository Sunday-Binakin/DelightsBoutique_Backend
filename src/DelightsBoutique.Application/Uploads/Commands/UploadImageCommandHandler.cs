using DelightsBoutique.Application.Abstractions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DelightsBoutique.Application.Uploads.Commands;

public class UploadImageCommandHandler(IImageValidator imageValidator, IImageProcessor imageProcessor, IFileStorage fileStorage)
    : IRequestHandler<UploadImageCommand, UploadImageResult>
{
    // Deliberately narrower than IImageValidator's "any decodable image" check used by
    // payment-proof upload — this endpoint's callers (product/content image forms) asked
    // for exactly these three, not whatever ImageSharp happens to be able to decode.
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<UploadImageResult> Handle(UploadImageCommand request, CancellationToken ct)
    {
        var extension = imageValidator.DetectImageExtension(request.FileBytes);
        if (extension is null || !AllowedExtensions.Contains(extension))
            throw new ValidationException([new ValidationFailure("file", "File must be a JPEG, PNG, or WebP image.")]);

        var processed = await imageProcessor.ResizeAsync(request.FileBytes, ct);

        using var stream = new MemoryStream(processed.Bytes);
        // Always .jpg — ResizeAsync always re-encodes as JPEG regardless of the input format.
        var url = await fileStorage.SaveAsync(stream, "uploads", ".jpg", ct);

        return new UploadImageResult(url, processed.Width, processed.Height);
    }
}
