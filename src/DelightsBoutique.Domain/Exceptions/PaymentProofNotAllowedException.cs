using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Domain.Exceptions;

public class PaymentProofNotAllowedException(string orderId, OrderStatus currentStatus)
    : DomainException($"Order '{orderId}' cannot accept a payment proof while status is '{currentStatus}'.")
{
    public string OrderId { get; } = orderId;
    public OrderStatus CurrentStatus { get; } = currentStatus;
}
