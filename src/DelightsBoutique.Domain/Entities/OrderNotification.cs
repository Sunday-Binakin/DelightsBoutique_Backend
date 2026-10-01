namespace DelightsBoutique.Domain.Entities;

/// <summary>
/// Auto-created server-side on every order creation — never client-writable
/// except the <see cref="Read"/> flag.
/// </summary>
public class OrderNotification
{
    public Guid Id { get; private set; }
    public string OrderId { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public decimal Subtotal { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool Read { get; private set; }

    protected OrderNotification() { }
}
