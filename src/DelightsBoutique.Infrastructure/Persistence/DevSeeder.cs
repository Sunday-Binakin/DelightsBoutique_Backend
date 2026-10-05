using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.Infrastructure.Persistence;

/// <summary>
/// Development-only (guarded at the call site in Program.cs, never runs in Staging/
/// Production) — gives frontend developers a known, fixed staff login plus a small
/// non-empty product catalog on a fresh local database, instead of starting from an
/// empty storefront and no way in besides the console-logged one-time Owner password.
/// Each piece checks for its own known identity before inserting, so it stays a no-op
/// once seeded and never duplicates data across repeated local restarts.
/// </summary>
public static class DevSeeder
{
    public const string DevStaffEmail = "staff@dev.local";
    public const string DevStaffPassword = "DevPassword123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        if (!await db.StaffMembers.AnyAsync(s => s.Email == DevStaffEmail))
        {
            var hasher = services.GetRequiredService<IPasswordHasher>();
            // Full permission set — this is the one account meant for exercising every
            // admin screen locally, distinct from the Owner account's own singleton bypass.
            db.StaffMembers.Add(StaffMember.Create(
                "Dev Staff", DevStaffEmail, null, Enum.GetValues<Permission>().ToList(), hasher.Hash(DevStaffPassword)));
        }

        if (!await db.Categories.AnyAsync(c => c.Slug == "dresses"))
            db.Categories.Add(Category.Create("dresses", "Dresses", Tone.Rose));
        if (!await db.Categories.AnyAsync(c => c.Slug == "shoes"))
            db.Categories.Add(Category.Create("shoes", "Shoes", Tone.Charcoal));
        if (!await db.Categories.AnyAsync(c => c.Slug == "bags"))
            db.Categories.Add(Category.Create("bags", "Bags", Tone.Gold));

        // Gated on the catalog as a whole, not per-slug — once any real product exists
        // (admin-created or otherwise), the storefront is no longer empty, so there's
        // nothing left for this demo catalog to do.
        if (!await db.Products.AnyAsync())
        {
            db.Products.Add(Product.Create(
                "floral-midi-dress", "Floral Midi Dress", "dresses",
                "A flowing midi dress in a soft floral print.",
                retailPrice: 180m, wholesalePrice: 140m, wholesaleMoq: 6, compareAtPrice: 220m,
                tone: Tone.Rose, badge: Badge.New, stockStatus: StockStatus.InStock,
                sizes: ["S", "M", "L"], colors: ["Red", "Blue"]));

            db.Products.Add(Product.Create(
                "classic-block-heels", "Classic Block Heels", "shoes",
                "Comfortable block heels for everyday elegance.",
                retailPrice: 220m, wholesalePrice: null, wholesaleMoq: null, compareAtPrice: null,
                tone: Tone.Charcoal, badge: Badge.BestSeller, stockStatus: StockStatus.InStock,
                sizes: ["38", "39", "40"], colors: ["Black", "Nude"]));

            db.Products.Add(Product.Create(
                "woven-tote-bag", "Woven Tote Bag", "bags",
                "A spacious woven tote for daily errands.",
                retailPrice: 150m, wholesalePrice: 110m, wholesaleMoq: 10, compareAtPrice: null,
                tone: Tone.Gold, badge: null, stockStatus: StockStatus.LowStock,
                colors: ["Tan"]));
        }

        await db.SaveChangesAsync();
    }
}
