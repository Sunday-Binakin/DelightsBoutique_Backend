using Microsoft.AspNetCore.Authorization;

namespace DelightsBoutique.Api.Filters;

/// <summary>Backed by a plain "OwnerOnly" policy registered in Program.cs's
/// AddAuthorization(...) call — PermissionPolicyProvider falls back to the default policy
/// provider for any policy name that isn't shaped "Permission:{value}", so no new
/// IAuthorizationRequirement/handler is needed for this.</summary>
public class RequireOwnerAttribute : AuthorizeAttribute
{
    public RequireOwnerAttribute() => Policy = "OwnerOnly";
}
