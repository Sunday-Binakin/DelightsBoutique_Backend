using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

public class UploadPaymentProofCommandHandler(IOrderRepository orders, IImageValidator imageValidator, IFileStorage fileStorage)
    : IRequestHandler<UploadPaymentProofCommand, OrderDto?>
{
    public async Task<OrderDto?> Handle(UploadPaymentProofCommand request, CancellationToken ct)
    {
        var order = await orders.GetEntityByIdAsync(request.OrderId, ct);
        if (order is null)
            return null; // controller maps null -> 404

        var extension = imageValidator.DetectImageExtension(request.FileBytes);
        if (extension is null)
            throw new ValidationException([new ValidationFailure("file", "File is not a valid image.")]);

        using var stream = new MemoryStream(request.FileBytes);
        var url = await fileStorage.SaveAsync(stream, "payment-proofs", extension, ct);

        order.SetPaymentProofUrl(url); // throws PaymentProofNotAllowedException (409) if not PendingConfirmation
        await orders.UpdateAsync(order, ct);

        return OrderDtoMapper.ToDto(order);
    }
}
