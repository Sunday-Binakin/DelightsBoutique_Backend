namespace DelightsBoutique.Domain.Entities;

public class NewsletterSubscriber
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public DateTime SubscribedAt { get; private set; }

    protected NewsletterSubscriber() { }
}
