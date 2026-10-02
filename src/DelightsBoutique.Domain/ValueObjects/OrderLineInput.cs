namespace DelightsBoutique.Domain.ValueObjects;

/// <summary>
/// An already-priced order line, resolved by the caller (Application, via
/// IOrderPricingService) before Order.Create is invoked — Domain never resolves price
/// itself. Named distinctly from OrderItem (the persisted entity) to avoid confusion.
/// </summary>
public record OrderLineInput(string Slug, string Name, int Quantity, decimal UnitPrice, string? Size, string? Color);
