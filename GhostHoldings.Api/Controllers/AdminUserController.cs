using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using GhostHoldings.Application.Features.Admin;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 🔥 In production, change this to [Authorize(Roles = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // 🔥 LOCK A USER
    [HttpPost("{userId}/lock")]
    public async Task<IActionResult> LockUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LockUserCommand(userId, true), cancellationToken);
        
        if (!result.Success) 
            return BadRequest(new { Error = result.Message });
            
        return Ok(new { Message = result.Message });
    }

    // 🔥 UNLOCK A USER
    [HttpPost("{userId}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LockUserCommand(userId, false), cancellationToken);
        
        if (!result.Success) 
            return BadRequest(new { Error = result.Message });
            
        return Ok(new { Message = result.Message });
    }
}