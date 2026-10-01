using System.Security.Cryptography;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Infrastructure.Auth;

public class EfOtpChallengeStore(AppDbContext db, IPasswordHasher passwordHasher, IClock clock) : IOtpChallengeStore
{
    private const int CodeExpiryMinutes = 10;
    private const int MaxAttempts = 5;

    public async Task<(string LoginToken, string Code)> CreateAsync(Guid userId, string role, CancellationToken ct)
    {
        var code = GenerateCode();
        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            LoginToken = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Role = role,
            HashedCode = passwordHasher.Hash(code),
            ExpiresAt = clock.UtcNow.AddMinutes(CodeExpiryMinutes),
            AttemptCount = 0,
            CreatedAt = clock.UtcNow
        };

        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        return (challenge.LoginToken, code);
    }

    public async Task<(OtpVerifyOutcome Outcome, OtpVerifySuccess? Success)> VerifyAsync(string loginToken, string code, CancellationToken ct)
    {
        var challenge = await db.OtpChallenges.FirstOrDefaultAsync(c => c.LoginToken == loginToken, ct);

        if (challenge is null || challenge.ConsumedAt is not null || challenge.ExpiresAt <= clock.UtcNow || challenge.AttemptCount >= MaxAttempts)
            return (OtpVerifyOutcome.InvalidOrExpired, null);

        if (!passwordHasher.Verify(code, challenge.HashedCode))
        {
            challenge.AttemptCount++;
            await db.SaveChangesAsync(ct);
            return (OtpVerifyOutcome.InvalidOrExpired, null);
        }

        challenge.ConsumedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        return (OtpVerifyOutcome.Succeeded, new OtpVerifySuccess(challenge.UserId, challenge.Role));
    }

    public async Task<(string Code, Guid UserId, string Role)?> ResendAsync(string loginToken, CancellationToken ct)
    {
        var challenge = await db.OtpChallenges.FirstOrDefaultAsync(c => c.LoginToken == loginToken, ct);
        if (challenge is null || challenge.ConsumedAt is not null)
            return null;

        var code = GenerateCode();
        challenge.HashedCode = passwordHasher.Hash(code);
        challenge.ExpiresAt = clock.UtcNow.AddMinutes(CodeExpiryMinutes);
        // AttemptCount is deliberately NOT reset — resetting it on resend would let
        // lockout be trivially bypassed by spamming resend.
        await db.SaveChangesAsync(ct);

        return (code, challenge.UserId, challenge.Role);
    }

    private static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
