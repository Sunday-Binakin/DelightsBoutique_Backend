using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DelightsBoutique.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` build an <see cref="AppDbContext"/> without spinning up the full
/// Api composition root (which could fail at design time if anything in Program.cs
/// needs config/services not present outside a real run). Reads a connection string
/// with elevated (migration-capable) DB privileges from an env var set locally — never
/// the low-privilege runtime connection string used by the app itself.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_MIGRATION_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the DB_MIGRATION_CONNECTION environment variable to a migration-capable " +
                "(DDL-privileged) Postgres connection string before running EF tooling.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
