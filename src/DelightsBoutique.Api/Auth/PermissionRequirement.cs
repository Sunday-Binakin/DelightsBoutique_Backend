using DelightsBoutique.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DelightsBoutique.Api.Auth;

/// <summary>Succeeds if the staff member holds ANY of these — e.g. the generic image-upload
/// endpoint is called from both the Products and Content admin forms, so it accepts either
/// ManageProducts or ManageContent rather than needing two separate routes.</summary>
public class PermissionRequirement(IReadOnlyList<Permission> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<Permission> Permissions { get; } = permissions;
}
