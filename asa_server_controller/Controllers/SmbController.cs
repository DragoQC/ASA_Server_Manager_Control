using asa_server_controller.Models.Cluster;
using asa_server_controller.Services;
using Microsoft.AspNetCore.Mvc;

namespace asa_server_controller.Controllers;

[ApiController]
[Route("api/smb")]
public sealed class SmbController(SmbService smbService) : ControllerBase
{
    [HttpGet("invite/{inviteKey}")]
    public async Task<IActionResult> GetShareConfig(string inviteKey, CancellationToken cancellationToken)
    {
        try
        {
            SmbShareInviteResponse response = await smbService.GetShareRequestAsync(inviteKey, cancellationToken);
            return Ok(response);
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
