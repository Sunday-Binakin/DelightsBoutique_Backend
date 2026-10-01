using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class CustomerFlagConfiguration : IEntityTypeConfiguration<CustomerFlag>
{
    public void Configure(EntityTypeBuilder<CustomerFlag> builder)
    {
        builder.HasKey(c => c.PhoneNormalized);
    }
}
