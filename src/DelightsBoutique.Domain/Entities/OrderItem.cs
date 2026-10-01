namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// Snapshot of a product at order time (slug/name/price survive the product being
/// renamed or deleted later) — never fetched or modified independent of its order.
/// </summary>
public class OrderItem
{
    public Guid Id { get; private set; }
    public string OrderId { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Size { get; private set; }
    public string? Color { get; private set; }

    protected OrderItem() { }
}
