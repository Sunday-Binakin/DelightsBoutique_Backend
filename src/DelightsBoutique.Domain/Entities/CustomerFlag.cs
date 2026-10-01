namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// Keyed by normalized phone. Everything else about a "customer" (order count,
/// total spent, last order date) is computed from <see cref="Order"/> at query
/// time, not stored — one source of truth, nothing to drift.
/// </summary>
public class CustomerFlag
{
    public string PhoneNormalized { get; private set; } = default!;
    public bool Wholesale { get; private set; }

    protected CustomerFlag() { }
}
