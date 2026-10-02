using DelightsBoutique.Domain.Entities;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class CustomerFlagTests
{
    [Fact]
    public void Create_SetsPhoneAndWholesale()
    {
        var flag = CustomerFlag.Create("0241234567", true);

        flag.PhoneNormalized.Should().Be("0241234567");
        flag.Wholesale.Should().BeTrue();
    }

    [Fact]
    public void SetWholesale_UpdatesFlag()
    {
        var flag = CustomerFlag.Create("0241234567", false);

        flag.SetWholesale(true);

        flag.Wholesale.Should().BeTrue();
    }
}
