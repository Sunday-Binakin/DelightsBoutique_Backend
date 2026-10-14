using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using Microsoft.Extensions.Configuration;

namespace DelightsBoutique.Infrastructure.Storage;

/// <summary>
/// DigitalOcean Spaces (S3-compatible object storage) behind IFileStorage — durable, survives
/// container redeploys/restarts, unlike LocalFileStorage's on-disk files. Spaces speaks the S3
/// API directly, so this uses the AWS S3 SDK pointed at a Spaces endpoint rather than a
/// DigitalOcean-specific client.
/// </summary>
public class SpacesFileStorage : IFileStorage
{
    private readonly IClock clock;
    private readonly string bucket;
    private readonly string publicBaseUrl;
    private readonly AmazonS3Client s3Client;

    public SpacesFileStorage(IConfiguration config, IClock clock)
    {
        this.clock = clock;

        // e.g. "https://fra1.digitaloceanspaces.com" — the region-level endpoint, not the
        // bucket's own virtual-hosted URL.
        var serviceUrl = config["Spaces:Endpoint"]
            ?? throw new InvalidOperationException("Spaces:Endpoint must be configured.");
        var accessKey = config["Spaces:AccessKey"]
            ?? throw new InvalidOperationException("Spaces:AccessKey must be configured.");
        var secretKey = config["Spaces:SecretKey"]
            ?? throw new InvalidOperationException("Spaces:SecretKey must be configured.");
        bucket = config["Spaces:Bucket"]
            ?? throw new InvalidOperationException("Spaces:Bucket must be configured.");

        // Optional: a custom domain or DO's CDN-fronted Spaces endpoint
        // (https://<bucket>.<region>.cdn.digitaloceanspaces.com) for faster reads. Falls back to
        // the bucket's own direct Spaces URL, derived from the same endpoint used to write.
        publicBaseUrl = config["Spaces:PublicBaseUrl"]
            ?? $"{serviceUrl.Replace("://", $"://{bucket}.")}";

        s3Client = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonS3Config { ServiceURL = serviceUrl });
    }

    public async Task<string> SaveAsync(Stream content, string folder, string fileExtension, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var key = $"{folder}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{fileExtension}";

        content.Position = 0;
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            // Uploaded files are public-by-design (same trust model as LocalFileStorage's
            // publicly-served /files route) — the API never serves these itself.
            CannedACL = S3CannedACL.PublicRead
        }, ct);

        return $"{publicBaseUrl.TrimEnd('/')}/{key}";
    }
}
