using DelightsBoutique.Application.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace DelightsBoutique.Infrastructure.Storage;

public class ImageSharpImageProcessor : IImageProcessor
{
    private const int MaxDimension = 1000;
    private const int JpegQuality = 82;

    public async Task<ProcessedImage> ResizeAsync(byte[] content, CancellationToken ct)
    {
        using var image = Image.Load(content);

        if (image.Width > MaxDimension || image.Height > MaxDimension)
        {
            // ResizeMode.Max preserves aspect ratio and only shrinks — never upscales a
            // smaller image, and the explicit Width/Height check above already skips this
            // entirely when neither dimension exceeds the cap.
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(MaxDimension, MaxDimension) }));
        }

        using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = JpegQuality }, ct);

        return new ProcessedImage(output.ToArray(), image.Width, image.Height);
    }
}
