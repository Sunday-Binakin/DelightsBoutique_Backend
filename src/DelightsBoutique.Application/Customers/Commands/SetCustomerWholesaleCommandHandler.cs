using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Customers.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Customers.Commands;

public class SetCustomerWholesaleCommandHandler(ICustomerFlagRepository customerFlags, ICustomerRepository customers, IPhoneNormalizer phoneNormalizer)
    : IRequestHandler<SetCustomerWholesaleCommand, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(SetCustomerWholesaleCommand request, CancellationToken ct)
    {
        var normalizedPhone = phoneNormalizer.Normalize(request.Phone);
        await customerFlags.UpsertAsync(normalizedPhone, request.Wholesale, ct);

        // Null if this phone has never placed an order — not a "customer" yet by this
        // system's own definition, even though the flag write above still succeeded.
        return await customers.GetByPhoneAsync(normalizedPhone, ct);
    }
}
