using System.Security.Cryptography;
using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Infrastructure;

public class OrderIdGenerator : IOrderIdGenerator
{
    private const string Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // Custom epoch keeps the base36-encoded timestamp short for decades (vs. Unix epoch),
    // staying well within Order.Id's HasMaxLength(20).
    private static readonly DateTimeOffset Epoch = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public string NewId()
    {
        var secondsSinceEpoch = (long)(DateTimeOffset.UtcNow - Epoch).TotalSeconds;
        // 6 random chars (36^6 ≈ 2.18 billion combinations), not 4 (36^4 ≈ 1.68 million) —
        // a same-second burst (e.g. a flash sale) with only 4 random chars showed real,
        // non-negligible collisions in testing (~30 per 10,000 same-second calls, matching
        // the birthday-paradox estimate), not the "astronomically unlikely" risk originally
        // assumed. 6 chars keeps that same scenario's collision probability negligible
        // while staying comfortably under Order.Id's HasMaxLength(20).
        return $"DB-{ToBase36(secondsSinceEpoch)}{RandomSuffix(6)}";
    }

    private static string ToBase36(long value)
    {
        if (value == 0) return "0";

        var chars = new List<char>();
        while (value > 0)
        {
            chars.Insert(0, Base36Chars[(int)(value % 36)]);
            value /= 36;
        }

        return new string(chars.ToArray());
    }

    private static string RandomSuffix(int length) =>
        new([.. Enumerable.Range(0, length).Select(_ => Base36Chars[RandomNumberGenerator.GetInt32(Base36Chars.Length)])]);
}
