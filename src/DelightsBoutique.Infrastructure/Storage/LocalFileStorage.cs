using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DelightsBoutique.Infrastructure.Storage;

/// <summary>
/// Local disk storage behind IFileStorage — a later swap to a real S3-compatible provider
/// only touches this implementation, never the handler/controller code that calls it.
/// </summary>
public class LocalFileStorage(IConfiguration config, IClock clock, IHostEnvironment env) : IFileStorage
{
    // Relative paths resolve against the content root, not the process's current directory —
    // the two differ whenever the app is launched from outside its own project directory, and
    // Program.cs's PhysicalFileProvider for serving these same files requires an absolute path.
    private string RootPath
    {
        get
        {
            var configured = config["FileStorage:RootPath"] ?? Path.Combine("App_Data", "uploads");
            return Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
        }
    }

    private string PublicBaseUrl => config["FileStorage:PublicBaseUrl"]
        ?? throw new InvalidOperationException("FileStorage:PublicBaseUrl must be configured.");

    public async Task<string> SaveAsync(Stream content, string folder, string fileExtension, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var relativeDir = Path.Combine(folder, now.ToString("yyyy"), now.ToString("MM"));
        var absoluteDir = Path.Combine(RootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir); // idempotent, no-op if already present

        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        var absolutePath = Path.Combine(absoluteDir, fileName);

        await using var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write);
        content.Position = 0;
        await content.CopyToAsync(fileStream, ct);

        var relativeUrlPath = $"{relativeDir.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        return $"{PublicBaseUrl.TrimEnd('/')}/files/{relativeUrlPath}";
    }
}
