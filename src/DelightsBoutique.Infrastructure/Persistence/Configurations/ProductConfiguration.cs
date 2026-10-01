using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.RetailPrice).HasColumnType("numeric(12,2)");
        builder.Property(p => p.WholesalePrice).HasColumnType("numeric(12,2)");
        builder.Property(p => p.CompareAtPrice).HasColumnType("numeric(12,2)");
        builder.Property(p => p.Rating).HasColumnType("numeric(3,2)");

        builder.Property(p => p.Sizes).HasColumnType("text[]");
        builder.Property(p => p.Colors).HasColumnType("text[]");
        builder.Property(p => p.ImageUrls).HasColumnType("text[]");
    }
}
