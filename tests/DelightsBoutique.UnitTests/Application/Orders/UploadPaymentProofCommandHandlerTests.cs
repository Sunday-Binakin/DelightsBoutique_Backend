using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Commands;
using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Enums;
using DelightsBoutique.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace DelightsBoutique.UnitTests.Application.Orders;

public class UploadPaymentProofCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IImageValidator> _imageValidator = new();
    private readonly Mock<IFileStorage> _fileStorage = new();

    private UploadPaymentProofCommandHandler CreateHandler() => new(_orders.Object, _imageValidator.Object, _fileStorage.Object);

    [Fact]
    public async Task Handle_HappyPath_SetsUrlAndReturnsUpdatedDto()
    {
        var order = Order.ForTesting(OrderStatus.PendingConfirmation, FulfillmentType.Pickup);
        _orders.Setup(o => o.GetEntityByIdAsync("DB-TEST", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _imageValidator.Setup(v => v.DetectImageExtension(It.IsAny<byte[]>())).Returns(".jpg");
        _fileStorage.Setup(f => f.SaveAsync(It.IsAny<Stream>(), "payment-proofs", ".jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/files/payment-proofs/2026/10/abc.jpg");

        var result = await CreateHandler().Handle(new UploadPaymentProofCommand("DB-TEST", [1, 2, 3]), CancellationToken.None);

        result!.PaymentProofUrl.Should().Be("https://example.com/files/payment-proofs/2026/10/abc.jpg");
        _orders.Verify(o => o.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongStatus_ThrowsPaymentProofNotAllowedException()
    {
        var order = Order.ForTesting(OrderStatus.Confirmed, FulfillmentType.Pickup);
        _orders.Setup(o => o.GetEntityByIdAsync("DB-TEST", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _imageValidator.Setup(v => v.DetectImageExtension(It.IsAny<byte[]>())).Returns(".jpg");

        var act = () => CreateHandler().Handle(new UploadPaymentProofCommand("DB-TEST", [1, 2, 3]), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentProofNotAllowedException>();
    }

    [Fact]
    public async Task Handle_NonImage_ThrowsValidationException()
    {
        var order = Order.ForTesting(OrderStatus.PendingConfirmation, FulfillmentType.Pickup);
        _orders.Setup(o => o.GetEntityByIdAsync("DB-TEST", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _imageValidator.Setup(v => v.DetectImageExtension(It.IsAny<byte[]>())).Returns((string?)null);

        var act = () => CreateHandler().Handle(new UploadPaymentProofCommand("DB-TEST", [1, 2, 3]), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_UnknownOrderId_ReturnsNull()
    {
        _orders.Setup(o => o.GetEntityByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await CreateHandler().Handle(new UploadPaymentProofCommand("missing", [1, 2, 3]), CancellationToken.None);

        result.Should().BeNull();
    }
}
