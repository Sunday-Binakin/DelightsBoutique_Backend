using DelightsBoutique.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DelightsBoutique.Api.Auth;

/// <summary>
/// Dynamically builds an <see cref="AuthorizationPolicy"/> for any policy name shaped
/// "Permission:{value}" — backs <see cref="RequirePermissionAttribute"/> so a new
/// Permission enum value never needs a matching AddPolicy() call registered by hand.
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private const string Prefix = "Permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal) &&
            Enum.TryParse<Permission>(policyName[Prefix.Length..], out var permission))
        {
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Not one of our dynamic permission policies — defer to the default provider
        // so any plain, pre-registered AddPolicy() calls in future phases still work.
        return _fallback.GetPolicyAsync(policyName);
    }
}
