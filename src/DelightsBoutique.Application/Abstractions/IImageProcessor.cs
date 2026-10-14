namespace DelightsBoutique.Application.Abstractions;

public record ProcessedImage(byte[] Bytes, int Width, int Height);

public interface IImageProcessor
{
    /// <summary>Resizes to a max 1000px long edge (no-op if already smaller) and re-encodes
    /// as JPEG at ~82% quality, regardless of the input format.</summary>
    Task<ProcessedImage> ResizeAsync(byte[] content, CancellationToken ct);
}
