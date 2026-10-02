using DelightsBoutique.Application.Common;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Application.Common;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Rose Scarf", "rose-scarf")]
    [InlineData("  Leading and Trailing  ", "leading-and-trailing")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    [InlineData("Special!!Chars??Here", "special-chars-here")]
    [InlineData("already-a-slug", "already-a-slug")]
    public void FromName_ProducesExpectedSlug(string name, string expected)
    {
        SlugGenerator.FromName(name).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    [InlineData("   ")]
    public void FromName_WithNoUsableCharacters_FallsBackToRandomId(string name)
    {
        var slug = SlugGenerator.FromName(name);

        slug.Should().NotBeNullOrWhiteSpace();
        slug.Length.Should().Be(8);
    }
}
