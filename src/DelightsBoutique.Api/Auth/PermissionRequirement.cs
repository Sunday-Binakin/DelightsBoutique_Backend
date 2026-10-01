using DelightsBoutique.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DelightsBoutique.Api.Auth;

public class PermissionRequirement(Permission permission) : IAuthorizationRequirement
{
    public Permission Permission { get; } = permission;
}
