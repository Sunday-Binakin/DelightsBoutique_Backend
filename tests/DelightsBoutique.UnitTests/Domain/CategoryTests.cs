using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;

namespace DelightsBoutique.UnitTests.Domain;

public class CategoryTests
{
    [Fact]
    public void EnsureDeletable_WhenReferencedByProduct_ThrowsCategoryInUseException()
    {
        var category = Category.ForTesting("test-category");

        var act = () => category.EnsureDeletable(isReferencedByAnyProduct: true);

        act.Should().Throw<CategoryInUseException>()
            .Which.CategorySlug.Should().Be("test-category");
    }

    [Fact]
    public void EnsureDeletable_WhenNotReferenced_DoesNotThrow()
    {
        var category = Category.ForTesting("test-category");

        var act = () => category.EnsureDeletable(isReferencedByAnyProduct: false);

        act.Should().NotThrow();
    }
}
