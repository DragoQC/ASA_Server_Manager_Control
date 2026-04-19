using managerwebapp.Models.Invitations;
using managerwebapp.Services;
using Microsoft.AspNetCore.Mvc;

namespace managerwebapp.Controllers;

[ApiController]
[Route("api/vpn")]
public sealed class VpnController(InvitationService invitationService) : ControllerBase
{
    [HttpGet("invite/{inviteKey}")]
    public async Task<IActionResult> GetInviteConfig(string inviteKey, CancellationToken cancellationToken)
    {
        try
        {
            InviteRemoteServerRequest request = await invitationService.ClaimInviteAsync(inviteKey, cancellationToken);
            return Ok(request);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = exception.Message
            });
        }
    }
}
