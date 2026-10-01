using System.Security.Cryptography;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DelightsBoutique.Infrastructure.Persistence;

/// <summary>
/// Runtime seeder, not EF <c>HasData</c> — HasData would bake a literal BCrypt hash
/// (derived from some plaintext password) into a committed migration file forever.
/// This checks "does an owner already exist?" and only then generates+hashes+inserts,
/// keeping the password out of git entirely. Idempotent; safe to run on every
/// Development startup. Guarded by IsDevelopment() at the call site in Program.cs.
/// </summary>
public static class OwnerSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        if (await db.OwnerProfiles.AnyAsync())
            return;

        var hasher = services.GetRequiredService<IPasswordHasher>();
        var password = GenerateSecurePassword();

        var owner = OwnerProfile.Create(
            name: "Store Owner",
            email: "binakin10@gmail.com",
            phone: null,
            passwordHash: hasher.Hash(password));

        db.OwnerProfiles.Add(owner);
        await db.SaveChangesAsync();

        var logger = services.GetRequiredService<ILogger<AppDbContext>>();
        logger.LogWarning(
            "Seeded OwnerProfile ({Email}). One-time password: {Password} — record this now, it will not be shown again.",
            owner.Email, password);
    }

    private static string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(20);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
