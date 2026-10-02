using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

public class UpdateOrderStatusCommandHandler(IOrderRepository orders) : IRequestHandler<UpdateOrderStatusCommand, AdminOrderDto?>
{
    public async Task<AdminOrderDto?> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await orders.GetEntityByIdAsync(request.Id, ct);
        if (order is null) return null;

        if (request.Status is not null)
            order.TransitionTo(request.Status.Value); // InvalidStatusTransitionException -> 400

        if (request.InternalNotes is not null)
            order.SetInternalNotes(request.InternalNotes);

        await orders.UpdateAsync(order, ct); // xmin concurrency check fires here -> DbUpdateConcurrencyException -> 409 on a stale write

        return OrderDtoMapper.ToAdminDto(order);
    }
}
