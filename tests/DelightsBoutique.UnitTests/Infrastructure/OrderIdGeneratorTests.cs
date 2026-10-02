using DelightsBoutique.Infrastructure;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Infrastructure;

public class OrderIdGeneratorTests
{
    private readonly OrderIdGenerator _generator = new();

    [Fact]
    public void NewId_StartsWithDbPrefix()
    {
        _generator.NewId().Should().StartWith("DB-");
    }

    [Fact]
    public void NewId_IsAtMostTwentyCharacters()
    {
        _generator.NewId().Length.Should().BeLessThanOrEqualTo(20); // matches OrderConfiguration's HasMaxLength(20)
    }

    [Fact]
    public void NewId_ManyCallsProduceUniqueValues()
    {
        var ids = Enumerable.Range(0, 10_000).Select(_ => _generator.NewId()).ToHashSet();

        ids.Should().HaveCount(10_000);
    }
}
