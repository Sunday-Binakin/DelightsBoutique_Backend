namespace DelightsBoutique.Application.Abstractions;

public interface IFileStorage
{
    /// <summary>Stores content under a logical folder ("payment-proofs", later "uploads"),
    /// returns an absolute HTTPS URL. Swapping to a real S3-compatible provider later only
    /// touches the Infrastructure implementation — this signature never changes.</summary>
    Task<string> SaveAsync(Stream content, string folder, string fileExtension, CancellationToken ct);
}
