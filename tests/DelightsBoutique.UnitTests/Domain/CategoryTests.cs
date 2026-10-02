using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
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

    [Fact]
    public void Create_SetsAllFields()
    {
        var category = Category.Create("accessories", "Accessories", Tone.Gold, "https://example.com/img.jpg");

        category.Slug.Should().Be("accessories");
        category.Name.Should().Be("Accessories");
        category.Tone.Should().Be(Tone.Gold);
        category.ImageUrl.Should().Be("https://example.com/img.jpg");
        category.CreatedAt.Should().Be(category.UpdatedAt);
    }

    [Fact]
    public void Update_WithNullFields_LeavesExistingValuesUnchanged()
    {
        var category = Category.Create("accessories", "Accessories", Tone.Gold, "https://example.com/img.jpg");

        category.Update();

        category.Name.Should().Be("Accessories");
        category.Tone.Should().Be(Tone.Gold);
        category.ImageUrl.Should().Be("https://example.com/img.jpg");
    }

    [Fact]
    public void Update_WithNonNullFields_OverwritesOnlyThoseFields()
    {
        var category = Category.Create("accessories", "Accessories", Tone.Gold, "https://example.com/img.jpg");

        category.Update(name: "New Name");

        category.Name.Should().Be("New Name");
        category.Tone.Should().Be(Tone.Gold); // untouched
        category.ImageUrl.Should().Be("https://example.com/img.jpg"); // untouched
    }
}
