using DelightsBoutique.Infrastructure;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Infrastructure;

public class PhoneNormalizerTests
{
    private readonly PhoneNormalizer _normalizer = new();

    [Fact]
    public void Normalize_ZeroPrefixed_ReturnsUnchanged()
    {
        _normalizer.Normalize("0241234567").Should().Be("0241234567");
    }

    [Fact]
    public void Normalize_PlusCountryCodePrefixed_ReturnsZeroPrefixedForm()
    {
        _normalizer.Normalize("+233241234567").Should().Be("0241234567");
    }

    [Fact]
    public void Normalize_CountryCodePrefixedNoPlus_ReturnsZeroPrefixedForm()
    {
        _normalizer.Normalize("233241234567").Should().Be("0241234567");
    }

    [Fact]
    public void Normalize_StripsNonDigitCharacters()
    {
        _normalizer.Normalize("024-123-4567").Should().Be("0241234567");
    }

    [Fact]
    public void Normalize_AllThreeFormsResolveToSameValue()
    {
        var a = _normalizer.Normalize("0241234567");
        var b = _normalizer.Normalize("+233241234567");
        var c = _normalizer.Normalize("233241234567");

        a.Should().Be(b).And.Be(c);
    }
}
