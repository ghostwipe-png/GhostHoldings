using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Deposits;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DepositController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepositController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateDeposit([FromBody] InitiateDepositRequest request, CancellationToken cancellationToken)
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
            var reference = await _mediator.Send(new InitiateDepositCommand(userId, request.Amount), cancellationToken);

            // 3. Return success (The user's phone should now be receiving the M-Pesa PIN prompt)
            return Ok(new 
            { 
                Message = "M-Pesa prompt sent to your phone. Please enter your PIN to complete the deposit.",
                Reference = reference,
                Amount = request.Amount
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

// Simple DTO for the request body
public record InitiateDepositRequest(decimal Amount);