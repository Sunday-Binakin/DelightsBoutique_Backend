using System.Text.Json;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.Infrastructure.Persistence;

/// <summary>
/// Parallel to OwnerSeeder, but runs in EVERY environment (not IsDevelopment()-guarded) —
/// the storefront needs non-null content to function at all. Checks per key whether a row
/// already exists and only inserts the missing ones — never calls UpsertAsync, which would
/// unconditionally overwrite real admin-edited content on every restart.
/// </summary>
public static class ContentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var existingKeys = (await db.SiteContents.Select(s => s.Key).ToListAsync()).ToHashSet();

        foreach (var (key, json) in DefaultContent())
            if (!existingKeys.Contains(key))
                db.SiteContents.Add(SiteContent.Create(key, json));

        await db.SaveChangesAsync();
    }

    private static IEnumerable<(SiteContentKey Key, string Json)> DefaultContent()
    {
        yield return (SiteContentKey.SiteSettings, JsonSerializer.Serialize(new SiteSettingsDto(
            Tagline: "Elegant fashion, delivered with care.",
            WhatsappNumber: "+233200000000",
            WhatsappMessage: "Hi! I'd like to ask about an order.",
            PhoneDisplay: "+233 20 000 0000",
            Address: "Accra, Ghana",
            Hours: "Mon-Sat, 9am-6pm",
            Announcement: "Welcome to Delight's Boutique!",
            BankTransferInstructions: "Account details available on request via WhatsApp.",
            MobileMoneyInstructions: "Mobile money details available on request via WhatsApp.",
            CashInstructions: "Pay in cash on delivery or pickup.",
            FacebookUrl: "", InstagramUrl: "", TiktokUrl: "",
            NotifyByEmail: true, NotificationEmail: "", NotifyByWhatsapp: true), ContentJsonOptions.Default));

        yield return (SiteContentKey.Banners, JsonSerializer.Serialize(new BannerSetDto(
            HeroMain: DefaultBanner("New Season", "Shop the Latest Arrivals", "Shop Now", "/products"),
            HeroSide1: DefaultBanner("Trending", "Best Sellers", "View", "/products?badge=BestSeller"),
            HeroSide2: DefaultBanner("Limited Time", "Flash Sale", "View Sale", "/flash-sale"),
            FullWidth: DefaultBanner("Delight's Boutique", "Elegant Fashion for Every Occasion", "Explore", "/products")),
            ContentJsonOptions.Default));

        yield return (SiteContentKey.FlashSale, JsonSerializer.Serialize(new FlashSaleContentDto(
            Eyebrow: "Limited Time", Title: "Flash Sale", EndsAt: DateTime.UtcNow.AddDays(7), ProductSlugs: []),
            ContentJsonOptions.Default));

        yield return (SiteContentKey.CollectionSection, JsonSerializer.Serialize(new CollectionSectionContentDto(
            Eyebrow: "Shop by Collection", Title: "Discover Your Style",
            NewArrivalsLabel: "New Arrivals", BestSellersLabel: "Best Sellers", WholesalePicksLabel: "Wholesale Picks",
            NewArrivalsSlugs: [], BestSellersSlugs: [], WholesalePicksSlugs: []),
            ContentJsonOptions.Default));

        // StorefrontCopy's real shape is defined in the existing frontend's storefront-copy.ts,
        // which this backend doesn't have access to — default to an empty object.
        yield return (SiteContentKey.StorefrontCopy, "{}");

        yield return (SiteContentKey.PagesContent, JsonSerializer.Serialize(new PagesContentDto(
            About: new PageContentDto("About Us", "Delight's Boutique offers elegant ladies' fashion, footwear, bags, and accessories at both retail and wholesale pricing."),
            Shipping: new PageContentDto("Shipping & Delivery", "We offer delivery and pickup options. Delivery times and costs are confirmed with you directly after checkout."),
            Returns: new PageContentDto("Return Policy", "Contact us via WhatsApp within 48 hours of delivery to discuss a return or exchange."),
            Terms: new PageContentDto("Terms & Conditions", "By placing an order with Delight's Boutique, you agree to our standard terms of sale."),
            Privacy: new PageContentDto("Privacy Policy", "We only use your contact information to process and fulfill your order."),
            Faqs: []),
            ContentJsonOptions.Default));

        yield return (SiteContentKey.Theme, JsonSerializer.Serialize(
            new ThemeDto(Primary: "#0550c2", Accent: "#ff7101"), ContentJsonOptions.Default));
    }

    private static BannerDto DefaultBanner(string eyebrow, string title, string ctaLabel, string href) =>
        new(eyebrow, title, ctaLabel, href, Tone.Rose, "dresses", ImageUrl: null, TextColor: null);
}
