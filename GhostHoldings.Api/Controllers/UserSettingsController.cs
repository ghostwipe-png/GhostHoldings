using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Settings;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserSettingsController(IMediator mediator) => _mediator = mediator;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpPost("set-pin")]
    public async Task<IActionResult> SetPin([FromBody] SetPinRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetPinCommand(GetUserId(), request.Pin), cancellationToken);
        return result.Success ? Ok(new { Message = result.Message }) : BadRequest(new { Error = result.Message });
    }

    [HttpPost("toggle-biometric")]
    public async Task<IActionResult> ToggleBiometric([FromBody] ToggleBiometricRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new EnableBiometricCommand(GetUserId(), request.Enable), cancellationToken);
        return result.Success ? Ok(new { Message = result.Message }) : BadRequest(new { Error = result.Message });
    }
}

public record SetPinRequest(string Pin);
public record ToggleBiometricRequest(bool Enable);