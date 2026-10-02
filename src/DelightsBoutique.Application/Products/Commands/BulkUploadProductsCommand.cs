using DelightsBoutique.Application.Products.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

// Deliberately NOT IIdempotentCommand — BUILD_SPEC's idempotency requirement is scoped to
// POST /api/orders and POST /api/admin/staff only, both of which create a single real-world
// side effect that must not duplicate on retry. A re-submitted bulk upload isn't that.
public record BulkUploadProductsCommand(byte[] FileBytes) : IRequest<BulkUploadResultDto>;
