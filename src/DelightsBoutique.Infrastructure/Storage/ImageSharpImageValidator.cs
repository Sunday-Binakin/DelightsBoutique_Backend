using DelightsBoutique.Application.Abstractions;
using SixLabors.ImageSharp;

namespace DelightsBoutique.Infrastructure.Storage;

public class ImageSharpImageValidator : IImageValidator
{
    public string? DetectImageExtension(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            var format = Image.DetectFormat(stream);
            return format is null ? null : "." + format.FileExtensions.First();
        }
        catch
        {
            return null; // not a decodable image — never trust the caller's Content-Type/filename
        }
    }
}
