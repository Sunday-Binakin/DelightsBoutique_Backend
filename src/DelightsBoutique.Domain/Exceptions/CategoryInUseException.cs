namespace DelightsBoutique.Domain.Exceptions;

public class CategoryInUseException(string categorySlug)
    : DomainException($"Category '{categorySlug}' cannot be deleted while products reference it.")
{
    public string CategorySlug { get; } = categorySlug;
}
