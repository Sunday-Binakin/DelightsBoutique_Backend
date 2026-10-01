using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class OrderNotificationConfiguration : IEntityTypeConfiguration<OrderNotification>
{
    public void Configure(EntityTypeBuilder<OrderNotification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Subtotal).HasColumnType("numeric(12,2)");
        builder.HasIndex(n => n.Read);
    }
}
