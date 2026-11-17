using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<SpacesFileStorage> logger;
    private readonly string bucket;
    private readonly string publicBaseUrl;
    private readonly AmazonS3Client s3Client;

    public SpacesFileStorage(IConfiguration config, IClock clock, ILogger<SpacesFileStorage> logger)
    {
        this.clock = clock;
        this.logger = logger;

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
            new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                // AWSSDK.S3 v4 defaults to virtual-hosted-style addressing
                // (https://<bucket>.fra1.digitaloceanspaces.com/<key>). DigitalOcean Spaces
                // (like most non-AWS S3-compatible stores) doesn't handle that the same way AWS
                // does — it can redirect or respond in a shape the SDK can't parse as a normal
                // S3 error, surfacing as an AmazonS3Exception with an empty message wrapping a
                // bare HttpErrorResponseException. Forcing path-style
                // (https://fra1.digitaloceanspaces.com/<bucket>/<key>) is the standard fix every
                // non-AWS S3-compatible provider's docs recommend.
                ForcePathStyle = true,
                // AWSSDK.S3 v4 also defaults both of these to WHEN_SUPPORTED, which adds an
                // x-amz-checksum-* trailer to every PutObject — another thing Spaces doesn't
                // support. WHEN_REQUIRED restores the pre-v4 behavior (checksums only where the
                // S3 spec mandates them).
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    public async Task<string> SaveAsync(Stream content, string folder, string fileExtension, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var key = $"{folder}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{fileExtension}";

        content.Position = 0;
        try
        {
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
        }
        catch (AmazonS3Exception ex)
        {
            // The SDK's own exception message is often empty/useless against a non-AWS
            // S3-compatible endpoint (see ForcePathStyle comment above) — log the fields that
            // actually carry Spaces' real error instead of only the generic rethrow.
            logger.LogError(ex,
                "Spaces PutObject failed: StatusCode={StatusCode} ErrorCode={ErrorCode} RequestId={RequestId} Bucket={Bucket} Key={Key}",
                ex.StatusCode, ex.ErrorCode, ex.RequestId, bucket, key);
            throw;
        }

        return $"{publicBaseUrl.TrimEnd('/')}/{key}";
    }
}
