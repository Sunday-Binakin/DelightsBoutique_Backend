using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DelightsBoutique.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasMaxLength(20); // app-generated, not ValueGeneratedOnAdd

        builder.Property(o => o.Subtotal).HasColumnType("numeric(12,2)");
        builder.HasIndex(o => o.Phone);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Postgres has no native rowversion type — the Npgsql-specific idiom maps the
        // concurrency token to the `xmin` system column instead of a real added column.
        // A plain `byte[] RowVersion` property + .IsRowVersion() is the SQL Server
        // pattern and silently becomes a no-op on Npgsql.
        // Marked obsolete in Npgsql 8.0.11 pointing at EF Core's generic IsRowVersion()/
        // [Timestamp] API, but verified against the generated migration SQL: this call
        // correctly omits a real "xmin" column (Postgres's own system column is used),
        // and the untested "recommended" replacement risks either adding a real column
        // named xmin (a reserved system-column name, which Postgres will reject) or
        // silently not mapping to the system column at all. Keeping the verified path.
#pragma warning disable CS0618
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618
    }
}
