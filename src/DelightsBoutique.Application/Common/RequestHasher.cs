using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DelightsBoutique.Application.Common;

public static class RequestHasher
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Hashes the entire command object as JSON — this intentionally includes the
    /// IdempotencyKey property itself, which is harmless but redundant (FindAsync already
    /// scopes the lookup to (key, endpoint), so the key can never meaningfully differ
    /// within one comparison).
    ///
    /// Stable only because commands using this today are plain records/lists all the way
    /// down — System.Text.Json serializes declared properties in declaration order, so the
    /// same logical command always produces the same JSON. This would NOT hold for a
    /// command carrying a Dictionary&lt;string,T&gt; or raw JsonElement (key/property order
    /// isn't guaranteed stable for those) — if a future IIdempotentCommand needs one, this
    /// hashing approach needs revisiting first.
    /// </summary>
    public static string Hash<TRequest>(TRequest request)
    {
        var json = JsonSerializer.Serialize(request, Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
