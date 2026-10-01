namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// Append-only business-level trail ("who changed what"), written server-side by
/// a cross-cutting pipeline behavior on every admin mutation — never client-writable.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Actor { get; private set; } = default!;
    public string Action { get; private set; } = default!;

    protected AuditEntry() { }

    public static AuditEntry Create(string actor, string action) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow,
        Actor = actor,
        Action = action
    };
}
