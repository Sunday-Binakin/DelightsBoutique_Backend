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

    /// <summary>Test-support only — no write path for CustomerFlag exists yet (the admin
    /// "flag wholesale" endpoint is Phase 4's, under Manage Customers).</summary>
    internal static CustomerFlag ForTesting(string phoneNormalized, bool wholesale) => new()
    {
        PhoneNormalized = phoneNormalized,
        Wholesale = wholesale
    };
}
