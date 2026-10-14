namespace DelightsBoutique.Application.Newsletter.Dtos;

public record NewsletterSubscriberDto(string Email, DateTime SubscribedAt);

public record SubscribeResult(bool IsNew, string Email, DateTime SubscribedAt);
