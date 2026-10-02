using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class SiteContentTests
{
    [Fact]
    public void Create_SetsKeyAndJson()
    {
        var content = SiteContent.Create(SiteContentKey.Banners, "{\"a\":1}");

        content.Key.Should().Be(SiteContentKey.Banners);
        content.ValueJson.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void UpdateValue_ReplacesJson()
    {
        var content = SiteContent.Create(SiteContentKey.Banners, "{\"a\":1}");

        content.UpdateValue("{\"a\":2}");

        content.ValueJson.Should().Be("{\"a\":2}");
    }
}
