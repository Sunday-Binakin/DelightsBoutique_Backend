using DelightsBoutique.Domain.Entities;

namespace DelightsBoutique.Domain.Interfaces;

/// <summary>
/// Pure, no I/O — takes already-resolved data (never a phone number or anything requiring
/// a DB lookup). The caller resolves "is this customer wholesale" beforehand.
/// </summary>
public interface IOrderPricingService
{
    decimal ResolveUnitPrice(Product product, int quantity, bool customerIsWholesale);
}
