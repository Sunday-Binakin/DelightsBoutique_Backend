using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Categories.Commands;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Categories;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categories = new();

    private DeleteCategoryCommandHandler CreateHandler() => new(_categories.Object);

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsFalse()
    {
        _categories.Setup(r => r.GetEntityBySlugAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var result = await CreateHandler().Handle(new DeleteCategoryCommand("missing"), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CategoryReferencedByProduct_ThrowsCategoryInUseException()
    {
        var category = Category.ForTesting("accessories");
        _categories.Setup(r => r.GetEntityBySlugAsync("accessories", It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(r => r.IsReferencedByAnyProductAsync("accessories", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new DeleteCategoryCommand("accessories"), CancellationToken.None);

        await act.Should().ThrowAsync<CategoryInUseException>();
        _categories.Verify(r => r.DeleteAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryNotReferenced_DeletesAndReturnsTrue()
    {
        var category = Category.ForTesting("accessories");
        _categories.Setup(r => r.GetEntityBySlugAsync("accessories", It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(r => r.IsReferencedByAnyProductAsync("accessories", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new DeleteCategoryCommand("accessories"), CancellationToken.None);

        result.Should().BeTrue();
        _categories.Verify(r => r.DeleteAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }
}
