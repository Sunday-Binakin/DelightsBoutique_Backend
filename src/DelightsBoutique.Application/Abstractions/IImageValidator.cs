namespace DelightsBoutique.Application.Abstractions;

public interface IImageValidator
{
    /// <summary>Sniffs actual file bytes — never trusts the client's declared Content-Type
    /// or filename. Returns the detected extension (e.g. ".jpg") if it's a real, decodable
    /// image format, or null if it isn't.</summary>
    string? DetectImageExtension(byte[] content);
}
