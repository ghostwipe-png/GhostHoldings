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
            var references = await _mediator.Send(new InitiateWithdrawalCommand(userId, request.Amount), cancellationToken);

            // 3. Return success. In a real app, we would now trigger the Paystack Transfer API here 
            // (or via a background worker like Hangfire/Quartz for reliability).
            return Ok(new 
            { 
                Message = $"Withdrawal initiated successfully. {references.Count} transfer(s) queued for processing.",
                References = references,
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