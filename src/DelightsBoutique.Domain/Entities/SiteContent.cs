using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// One row per content key (banners, flash sale, site settings, ...) instead of
/// seven near-empty tables. Domain stays JSON-structure-free — the raw JSON string
/// is serialized/deserialized to its typed shape at the Application layer.
/// </summary>
public class SiteContent
{
    public Guid Id { get; private set; }
    public SiteContentKey Key { get; private set; }
    public string ValueJson { get; private set; } = default!;

    protected SiteContent() { }

    public static SiteContent Create(SiteContentKey key, string valueJson) => new()
    {
        Id = Guid.NewGuid(),
        Key = key,
        ValueJson = valueJson
    };

    public void UpdateValue(string valueJson) => ValueJson = valueJson;
}
