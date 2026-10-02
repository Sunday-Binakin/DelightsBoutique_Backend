using DelightsBoutique.Domain.Entities;
using DelightsBoutique.Domain.Interfaces;

namespace DelightsBoutique.Application.Orders.Services;

public class OrderPricingService : IOrderPricingService
{
    public decimal ResolveUnitPrice(Product product, int quantity, bool customerIsWholesale) =>
        customerIsWholesale && product.WholesalePrice.HasValue && product.WholesaleMoq.HasValue && quantity >= product.WholesaleMoq.Value
            ? product.WholesalePrice.Value
            : product.RetailPrice;
}
