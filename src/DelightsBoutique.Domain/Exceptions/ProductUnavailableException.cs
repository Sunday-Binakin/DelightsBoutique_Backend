namespace DelightsBoutique.Domain.Exceptions;

public class ProductUnavailableException(string slug)
    : DomainException($"Product '{slug}' is out of stock.")
{
    public string Slug { get; } = slug;
}
