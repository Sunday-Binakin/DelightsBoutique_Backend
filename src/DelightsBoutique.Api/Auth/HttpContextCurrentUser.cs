using System.Security.Claims;
using DelightsBoutique.Application.Abstractions;

namespace DelightsBoutique.Api.Auth;

/// <summary>
/// Lives in Api, not Infrastructure — touches IHttpContextAccessor/ClaimsPrincipal
/// (ASP.NET Core HTTP types), and Infrastructure must stay host-agnostic.
/// </summary>
public class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Name => User?.FindFirst("name")?.Value;
    public string? Role => User?.FindFirst("role")?.Value;

    public Guid? StaffId =>
        Role == "Staff" && Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
}
