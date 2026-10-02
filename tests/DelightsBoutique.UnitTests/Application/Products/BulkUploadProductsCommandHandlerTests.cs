using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Commands;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Products;

public class BulkUploadProductsCommandHandlerTests
{
    private readonly Mock<IProductCsvParser> _csvParser = new();
    private readonly Mock<IMediator> _mediator = new();

    private BulkUploadProductsCommandHandler CreateHandler() => new(_csvParser.Object, _mediator.Object);

    private static ProductDto SampleDto(string slug) => new(
        Guid.NewGuid(), slug, "Name", "cat", "Category", "desc", 50m, null, null, null,
        0m, 0, Tone.Rose, null, StockStatus.InStock, [], [], [], DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task Handle_ParseError_RecordsRowFailureWithoutCallingMediator()
    {
        _csvParser.Setup(p => p.Parse(It.IsAny<byte[]>())).Returns([
            new ProductCsvRow(1, null, "Name", "cat", "desc", 0m, null, null, null, Tone.Rose, null, StockStatus.InStock, null, null, null, "retailPrice 'abc' is not a valid number.")
        ]);

        var result = await CreateHandler().Handle(new BulkUploadProductsCommand([]), CancellationToken.None);

        result.ErrorCount.Should().Be(1);
        result.Rows[0].Error.Should().Contain("retailPrice");
        _mediator.Verify(m => m.Send(It.IsAny<IRequest<ProductDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MixOfValidAndInvalidRows_ReportsPerRowNotAllOrNothing()
    {
        _csvParser.Setup(p => p.Parse(It.IsAny<byte[]>())).Returns([
            new ProductCsvRow(1, null, "Good Product", "cat", "desc", 50m, null, null, null, Tone.Rose, null, StockStatus.InStock, null, null, null, null),
            new ProductCsvRow(2, null, "Bad Product", "cat", "desc", 50m, null, null, null, Tone.Rose, null, StockStatus.InStock, null, null, null, null)
        ]);

        _mediator.Setup(m => m.Send(It.Is<CreateProductCommand>(c => c.Name == "Good Product"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleDto("good-product"));
        _mediator.Setup(m => m.Send(It.Is<CreateProductCommand>(c => c.Name == "Bad Product"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException([new ValidationFailure("categorySlug", "Category 'cat' does not exist.")]));

        var result = await CreateHandler().Handle(new BulkUploadProductsCommand([]), CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.Rows.Should().ContainSingle(r => r.Success && r.Slug == "good-product");
        result.Rows.Should().ContainSingle(r => !r.Success && r.Error!.Contains("does not exist"));
    }

    [Fact]
    public async Task Handle_DuplicateSlug_RecordsRowFailure()
    {
        _csvParser.Setup(p => p.Parse(It.IsAny<byte[]>())).Returns([
            new ProductCsvRow(1, "existing-slug", "Name", "cat", "desc", 50m, null, null, null, Tone.Rose, null, StockStatus.InStock, null, null, null, null)
        ]);
        _mediator.Setup(m => m.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateSlugException("existing-slug"));

        var result = await CreateHandler().Handle(new BulkUploadProductsCommand([]), CancellationToken.None);

        result.ErrorCount.Should().Be(1);
        result.Rows[0].Error.Should().Contain("existing-slug");
    }
}
