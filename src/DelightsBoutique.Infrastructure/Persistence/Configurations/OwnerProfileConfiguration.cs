using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class OwnerProfileConfiguration : IEntityTypeConfiguration<OwnerProfile>
{
    public void Configure(EntityTypeBuilder<OwnerProfile> builder)
    {
        builder.ToTable("owner_profile");
        builder.HasKey(o => o.Id);
        // No DB-level singleton constraint — nothing in the API ever creates a second
        // owner; the seeder's own empty-table check is sufficient (see OwnerSeeder).
    }
}
