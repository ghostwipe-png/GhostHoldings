using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Withdrawals;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WithdrawalController : ControllerBase
{
    private readonly IMediator _mediator;

    public WithdrawalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateWithdrawal([FromBody] InitiateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        // 1. Extract User ID from JWT
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { Error = "Invalid token." });
        }

        try
        {
            // 2. Send command to Application layer 
            // 🔥 FIXED: Amount first, then UserId (matches the Command record definition)
            var result = await _mediator.Send(new InitiateWithdrawalCommand(request.Amount, userId), cancellationToken);

            // 3. Return success response
            return Ok(new 
            { 
                Message = result.Message,
                Reference = result.Reference,
                Amount = request.Amount
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public record InitiateWithdrawalRequest(decimal Amount);