using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staff");
        builder.HasKey(s => s.Id);

        // Case-insensitive uniqueness via a lowercase-normalized value (BUILD_SPEC §5
        // allows this or citext — this avoids an extra Postgres extension dependency).
        // Email must always be written lowercased by the caller.
        builder.HasIndex(s => s.Email).IsUnique();

        builder.Property(s => s.Permissions).HasColumnType("integer[]");
    }
}
