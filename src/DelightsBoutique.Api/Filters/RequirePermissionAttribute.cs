using DelightsBoutique.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DelightsBoutique.Api.Filters;

/// <summary>Succeeds if the caller holds ANY of the given permissions — `params` keeps every
/// existing single-permission call site (`[RequirePermission(Permission.ManageProducts)]`)
/// unchanged.</summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public IReadOnlyList<Permission> Permissions { get; }

    public RequirePermissionAttribute(params Permission[] permissions)
    {
        Permissions = permissions;
        Policy = $"Permission:{string.Join(",", permissions)}"; // enum names never contain ',', safe separator
    }
}
