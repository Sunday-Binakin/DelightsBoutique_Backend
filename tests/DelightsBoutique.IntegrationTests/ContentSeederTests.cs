using System.Net;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DelightsBoutique.IntegrationTests;

public class ContentSeederTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateRows_AndDoesNotOverwriteEditedContent()
    {
        var admin = await TestAuthHelper.LoginAsAsync(factory, CustomWebApplicationFactory.ManageContentStaffEmail);
        var editedTagline = $"Edited by admin {Guid.NewGuid():N}";
        (await admin.PatchAsync("/api/admin/content/site-settings", new { tagline = editedTagline }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ContentSeeder.SeedAsync(scope.ServiceProvider); // run again, simulating a second app startup

        var siteSettingsRows = await db.SiteContents.Where(s => s.Key == SiteContentKey.SiteSettings).CountAsync();
        siteSettingsRows.Should().Be(1); // no duplicate row

        var response = await new AuthTestClient(factory.CreateClient()).GetAsync("/api/content/site-settings");
        var body = await TestAuthHelper.ReadJsonAsync(response);
        body.GetProperty("tagline").GetString().Should().Be(editedTagline); // not stomped by re-seeding
    }
}
