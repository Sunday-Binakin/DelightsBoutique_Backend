namespace DelightsBoutique.Domain.Enums;

public enum OrderStatus
{
    PendingConfirmation,
    Confirmed,
    Processing,
    OutForDelivery,
    ReadyForPickup,
    Completed,
    Cancelled
}
