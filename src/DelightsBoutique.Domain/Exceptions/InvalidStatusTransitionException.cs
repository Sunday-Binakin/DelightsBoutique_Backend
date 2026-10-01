using DelightsBoutique.Domain.Enums;

namespace DelightsBoutique.Domain.Exceptions;

public class InvalidStatusTransitionException(OrderStatus from, OrderStatus to)
    : DomainException($"Cannot transition order from '{from}' to '{to}'.")
{
    public OrderStatus From { get; } = from;
    public OrderStatus To { get; } = to;
}
