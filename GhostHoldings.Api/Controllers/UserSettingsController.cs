using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.UserSettings; // 🔥 Updated to match the new namespace

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserSettingsController(IMediator mediator) => _mediator = mediator;

    // 🔥 Clean helper to get the authenticated user's ID
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpPost("set-pin")]
    public async Task<IActionResult> SetPin([FromBody] SetPinRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 🔥 Sends the SetPinCommand we created earlier
            var result = await _mediator.Send(new SetPinCommand(GetUserId(), request.Pin), cancellationToken);
            return Ok(result); // Returns { "message": "...", "error": null }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("toggle-biometric")]
    public async Task<IActionResult> ToggleBiometric([FromBody] ToggleBiometricRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 🔥 Sends the ToggleBiometricCommand we created earlier
            var result = await _mediator.Send(new ToggleBiometricCommand(GetUserId(), request.Enable), cancellationToken);
            return Ok(result); // Returns { "message": "...", "error": null }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

// 🔥 DTOs for the Controller
public record SetPinRequest(string Pin);
public record ToggleBiometricRequest(bool Enable);