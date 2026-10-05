using DelightsBoutique.Application.Account.Commands;
using DelightsBoutique.Application.Account.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DelightsBoutique.Api.Controllers;

// Self-service only — any authenticated Owner or Staff can act on their own account, no
// Permission required. The command handlers resolve "which account" from the session
// itself (ICurrentUser), never from a request body id, so this can never touch anyone else's.
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController(IMediator mediator) : ControllerBase
{
    // Session's own claims (GET /api/auth/session) don't carry Phone — this is the live,
    // always-current alternative, not a static cookie snapshot.
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await mediator.Send(new GetAccountQuery(), ct));

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateAccountProfileCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }
}
