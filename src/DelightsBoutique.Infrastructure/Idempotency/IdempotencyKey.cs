namespace DelightsBoutique.Infrastructure.Idempotency;

/// <summary>
/// Infrastructure-only — has no business meaning outside idempotency plumbing, so it
/// doesn't belong in Domain. Schema only this phase; the MediatR pipeline behavior that
/// actually uses this table is Phase 3's.
/// </summary>
public class IdempotencyKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Endpoint { get; set; } = default!;
    public string RequestHash { get; set; } = default!;
    public int ResponseStatus { get; set; }
    public string ResponseBody { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
