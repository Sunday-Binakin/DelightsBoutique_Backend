namespace DelightsBoutique.Domain.Exceptions;

public class DuplicateSlugException(string slug)
    : DomainException($"Slug '{slug}' is already in use.")
{
    public string Slug { get; } = slug;
}
