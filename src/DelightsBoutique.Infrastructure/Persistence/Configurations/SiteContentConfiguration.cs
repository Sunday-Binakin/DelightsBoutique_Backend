using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class SiteContentConfiguration : IEntityTypeConfiguration<SiteContent>
{
    public void Configure(EntityTypeBuilder<SiteContent> builder)
    {
        builder.ToTable("site_content");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.Key).IsUnique();
        builder.Property(s => s.ValueJson).HasColumnName("value").HasColumnType("jsonb");
    }
}
