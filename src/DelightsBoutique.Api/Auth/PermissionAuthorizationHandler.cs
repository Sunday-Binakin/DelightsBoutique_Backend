using System.Security.Claims;
using DelightsBoutique.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DelightsBoutique.Api.Auth;

/// <summary>
/// Registered scoped so it can inject a scoped AppDbContext — it re-checks
/// StaffMember.Active against the DATABASE on every request (not just the cached
/// cookie claim), per BUILD_SPEC §7: a deactivated staff member is locked out
/// immediately, not just at next login.
/// </summary>
public class PermissionAuthorizationHandler(AppDbContext db) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("role", "Owner"))
        {
            context.Succeed(requirement);
            return;
        }

        var staffIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (staffIdClaim is null || !Guid.TryParse(staffIdClaim, out var staffId))
            return; // fails closed

        var staff = await db.StaffMembers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffId);
        if (staff is null || !staff.Active)
            return;

        if (requirement.Permissions.Any(staff.Permissions.Contains))
            context.Succeed(requirement);
    }
}
