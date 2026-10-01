using DelightsBoutique.Application.Common;

namespace DelightsBoutique.IntegrationTests;

/// <summary>Settable clock so OTP expiry can be tested deterministically, no real waiting.</summary>
public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}
