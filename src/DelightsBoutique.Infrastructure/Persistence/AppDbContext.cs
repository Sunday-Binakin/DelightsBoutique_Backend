using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Infrastructure.Auth;
using DelightsBoutique.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CustomerFlag> CustomerFlags => Set<CustomerFlag>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<OwnerProfile> OwnerProfiles => Set<OwnerProfile>();
    public DbSet<OrderNotification> OrderNotifications => Set<OrderNotification>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SiteContent> SiteContents => Set<SiteContent>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
