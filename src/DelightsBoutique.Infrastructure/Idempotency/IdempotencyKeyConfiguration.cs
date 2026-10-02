using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Idempotency;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.HasKey(k => k.Id);
        // Unique, not just indexed: a non-unique index doesn't stop two genuinely
        // concurrent fresh requests (same brand-new key, e.g. a double-click across two
        // tabs) from both passing the "not found" check and both inserting — defeating
        // idempotency's actual purpose under real concurrency.
        builder.HasIndex(k => new { k.Key, k.Endpoint }).IsUnique();
        builder.HasIndex(k => k.CreatedAt);
    }
}
