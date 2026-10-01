using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Idempotency;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.HasIndex(k => new { k.Key, k.Endpoint });
        builder.HasIndex(k => k.CreatedAt);
    }
}
