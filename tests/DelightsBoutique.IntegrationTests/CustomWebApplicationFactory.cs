using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace DelightsBoutique.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string OwnerEmail = "owner@test.local";
    public const string ActiveStaffEmail = "staff-active@test.local";
    public const string DeactivatedStaffEmail = "staff-deactivated@test.local";
    public const string ManageProductsStaffEmail = "staff-manage-products@test.local";
    public const string ManageContentStaffEmail = "staff-manage-content@test.local";
    public const string ManageCustomersStaffEmail = "staff-manage-customers@test.local";
    public const string ViewReportsStaffEmail = "staff-view-reports@test.local";
    public const string KnownPassword = "Password123!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    // Explicit absolute temp path — WebApplicationFactory's ambient content-root resolves
    // relative to the test assembly's output location, not the Api project, so relying on
    // a relative default would be fragile. Unique per factory instance for test isolation.
    private readonly string _fileStorageRoot = Path.Combine(Path.GetTempPath(), "delights-uploads-" + Guid.NewGuid().ToString("N"));

    public FakeOtpEmailSender OtpEmailSender { get; } = new();
    public FakeClock Clock { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await ContentSeeder.SeedAsync(scope.ServiceProvider); // mirrors Program.cs's own seeding, after migration

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(KnownPassword);

        db.OwnerProfiles.Add(OwnerProfile.Create("Test Owner", OwnerEmail, null, passwordHash));
        db.StaffMembers.Add(StaffMember.ForTesting(ActiveStaffEmail, passwordHash, active: true, [Permission.ManageOrders]));
        db.StaffMembers.Add(StaffMember.ForTesting(DeactivatedStaffEmail, passwordHash, active: false, [Permission.ManageOrders]));
        db.StaffMembers.Add(StaffMember.ForTesting(ManageProductsStaffEmail, passwordHash, active: true, [Permission.ManageProducts]));
        db.StaffMembers.Add(StaffMember.ForTesting(ManageContentStaffEmail, passwordHash, active: true, [Permission.ManageContent]));
        db.StaffMembers.Add(StaffMember.ForTesting(ManageCustomersStaffEmail, passwordHash, active: true, [Permission.ManageCustomers]));
        db.StaffMembers.Add(StaffMember.ForTesting(ViewReportsStaffEmail, passwordHash, active: true, [Permission.ViewReports]));
        await db.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // so the Development-only OwnerSeeder does NOT run

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["FileStorage:RootPath"] = _fileStorageRoot,
                ["FileStorage:PublicBaseUrl"] = "https://testserver"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOtpEmailSender>();
            services.AddSingleton<IOtpEmailSender>(OtpEmailSender);

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        if (Directory.Exists(_fileStorageRoot))
            Directory.Delete(_fileStorageRoot, recursive: true);
        await base.DisposeAsync();
    }
}
