using System.Security.Cryptography;
using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Infrastructure;

/// <summary>Same approach as OwnerSeeder's one-off password generation, extracted here so
/// staff creation/reset can reuse it instead of duplicating the character set and length.</summary>
public class PasswordGenerator : IPasswordGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return new string(bytes.Select(b => Chars[b % Chars.Length]).ToArray());
    }
}
