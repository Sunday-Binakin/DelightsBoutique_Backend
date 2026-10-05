namespace DelightsBoutique.Domain.Entities;

public class NewsletterSubscriber
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public DateTime SubscribedAt { get; private set; }

    protected NewsletterSubscriber() { }

    // Normalized to lowercase — same convention as OwnerProfile/StaffMember email lookups —
    // so "Foo@Bar.com" and "foo@bar.com" dedup as the same subscriber, not two rows.
    public static NewsletterSubscriber Create(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email.ToLowerInvariant(),
        SubscribedAt = DateTime.UtcNow
    };
}
