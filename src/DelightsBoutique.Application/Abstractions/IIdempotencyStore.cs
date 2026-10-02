namespace DelightsBoutique.Application.Abstractions;

public record IdempotencyKeyLookup(string RequestHash, int ResponseStatus, string ResponseBody);

public interface IIdempotencyStore
{
    Task<IdempotencyKeyLookup?> FindAsync(string key, string endpoint, CancellationToken ct);
    Task SaveAsync(string key, string endpoint, string requestHash, int responseStatus, string responseBody, CancellationToken ct);
}
