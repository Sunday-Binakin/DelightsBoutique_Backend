using DelightsBoutique.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DelightsBoutique.Api.Filters;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public Permission Permission { get; }

    public RequirePermissionAttribute(Permission permission)
    {
        Permission = permission;
        Policy = $"Permission:{permission}";
    }
}
