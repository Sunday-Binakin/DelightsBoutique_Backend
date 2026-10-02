using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Infrastructure;

/// <summary>
/// Pure logic, no I/O — lives in Infrastructure (not Application) following the
/// SystemClock precedent, registered via DI for testability/consistency.
///
/// Canonical form: 0-prefixed local Ghana number (e.g. "0241234567"). Any "+233..." or
/// "233..." form (12 digits after stripping non-digits) normalizes by replacing the
/// leading "233" with "0". An already-0-prefixed number passes through unchanged. This
/// single rule is reused for: the wholesale lookup in checkout, track-by-phone, and
/// (Phase 4) customer aggregation — Order.Phone always stores the normalized form.
/// </summary>
public class PhoneNormalizer : IPhoneNormalizer
{
    public string Normalize(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone)) return string.Empty;

        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("233") && digits.Length == 12)
            return "0" + digits[3..];

        return digits;
    }
}
