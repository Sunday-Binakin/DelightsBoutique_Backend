using DelightsBoutique.Application.Common;

namespace DelightsBoutique.Infrastructure;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
