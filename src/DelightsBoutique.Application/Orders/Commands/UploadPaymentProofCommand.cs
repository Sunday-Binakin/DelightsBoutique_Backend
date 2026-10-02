using DelightsBoutique.Application.Orders.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Orders.Commands;

// Not IIdempotentCommand — BUILD_SPEC §8 only requires idempotency on POST /api/orders
// and POST /api/admin/staff; this endpoint isn't in that list.
public record UploadPaymentProofCommand(string OrderId, byte[] FileBytes) : IRequest<OrderDto?>;
