using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.Infrastructure.Persistence;

/// <summary>
/// Development-only (guarded at the call site in Program.cs, never runs in Staging/
/// Production) — gives frontend developers a known, fixed staff login, one single-
/// permission account per Permission value for exercising permission boundaries locally,
/// and a product catalog large enough to exercise pagination, instead of starting from an
/// empty storefront and no way in besides the console-logged one-time Owner password.
/// Each piece checks for its own known identity before inserting, so it stays a no-op
/// once seeded and never duplicates data across repeated local restarts.
/// </summary>
public static class DevSeeder
{
    public const string DevStaffEmail = "staff@dev.local";
    public const string DevStaffPassword = "DevPassword123!";

    private const int DemoProductCount = 120;

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();

        if (!await db.StaffMembers.AnyAsync(s => s.Email == DevStaffEmail))
        {
            // Full permission set — this is the one account meant for exercising every
            // admin screen locally, distinct from the Owner account's own singleton bypass.
            db.StaffMembers.Add(StaffMember.Create(
                "Dev Staff", DevStaffEmail, null, Enum.GetValues<Permission>().ToList(), hasher.Hash(DevStaffPassword)));
        }

        // One account per permission, holding ONLY that permission — lets the frontend
        // verify each permission boundary (a 403 on everything else) without juggling a
        // single account's assigned set back and forth through the admin UI.
        foreach (var permission in Enum.GetValues<Permission>())
        {
            var email = $"staff-{permission.ToString().ToLowerInvariant()}@dev.local";
            if (!await db.StaffMembers.AnyAsync(s => s.Email == email))
                db.StaffMembers.Add(StaffMember.Create($"Dev {permission}", email, null, [permission], hasher.Hash(DevStaffPassword)));
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

            foreach (var product in GenerateDemoCatalog())
                db.Products.Add(product);
        }

        await db.SaveChangesAsync();
    }

    // Generated, not hand-written — 120 is enough to exercise pagination (default page
    // size 20 → 6+ pages) without hand-authoring each row. Deterministic slugs/names so a
    // re-seed (on an empty DB) always produces the same catalog.
    private static IEnumerable<Product> GenerateDemoCatalog()
    {
        var categories = new[] { ("dresses", Tone.Rose), ("shoes", Tone.Charcoal), ("bags", Tone.Gold) };
        var adjectives = new[] { "Classic", "Elegant", "Modern", "Vintage", "Chic", "Casual", "Bold", "Soft" };
        var nouns = new[] { "Dress", "Heel", "Tote", "Sandal", "Gown", "Clutch", "Wrap", "Pump" };

        for (var i = 1; i <= DemoProductCount - 3; i++)
        {
            var (categorySlug, tone) = categories[i % categories.Length];
            var price = 50m + i % 20 * 15m;
            var badge = i % 7 == 0 ? Badge.BestSeller : i % 5 == 0 ? Badge.New : (Badge?)null;
            var stockStatus = i % 11 == 0 ? StockStatus.OutOfStock : i % 6 == 0 ? StockStatus.LowStock : StockStatus.InStock;

            yield return Product.Create(
                $"dev-product-{i}", $"{adjectives[i % adjectives.Length]} {nouns[i % nouns.Length]} {i}", categorySlug,
                $"Demo product #{i}, generated for local pagination/testing.",
                retailPrice: price,
                wholesalePrice: i % 3 == 0 ? price * 0.75m : null,
                wholesaleMoq: i % 3 == 0 ? 5 : null,
                compareAtPrice: i % 8 == 0 ? price * 1.3m : null,
                tone: tone, badge: badge, stockStatus: stockStatus,
                sizes: ["S", "M", "L"], colors: ["Black", "Red"]);
        }
    }
}
