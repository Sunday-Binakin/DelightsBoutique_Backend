using System.Security.Claims;
using DelightsBoutique.Application.Auth.Commands;
using DelightsBoutique.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DelightsBoutique.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result switch
        {
            LoginSucceeded s => Ok(new { loginToken = s.LoginToken }),
            LoginStaffDeactivated => StatusCode(StatusCodes.Status403Forbidden,
                new { error = new { code = "STAFF_DEACTIVATED", message = "This staff account is deactivated." } }),
            _ => Unauthorized()
        };
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result is not VerifyOtpSucceeded success)
            return Unauthorized();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, success.UserId.ToString()),
            new("role", success.Role),
            new(ClaimTypes.Email, success.Email),
            new("name", success.Name)
        };
        claims.AddRange(success.Permissions.Select(p => new Claim("permission", p.ToString())));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { role = success.Role, staffId = success.StaffId, email = success.Email, name = success.Name });
    }

    [HttpPost("resend-otp")]
    [EnableRateLimiting("auth-resend-otp")]
    public async Task<IActionResult> ResendOtp(ResendOtpCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result is ResendOtpSucceeded ? Ok() : Unauthorized();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("session")]
    [Authorize]
    public IActionResult Session()
    {
        var role = User.FindFirst("role")?.Value ?? "";
        var staffIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new
        {
            role,
            staffId = role == "Staff" ? staffIdClaim : null,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            name = User.FindFirst("name")?.Value
        });
    }
}
