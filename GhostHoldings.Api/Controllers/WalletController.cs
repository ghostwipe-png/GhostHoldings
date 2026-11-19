using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Wallet;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] 
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetWalletBalance(CancellationToken cancellationToken)
    {
        // 1. Extract the User ID from the JWT Token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { Error = "Invalid token." });
        }

        try
        {
            // 2. Send query to Application layer
            var wallet = await _mediator.Send(new GetWalletQuery(userId), cancellationToken);
            return Ok(wallet);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            // 🔥 TEMPORARY DEBUG: Expose the exact C# error to the Kotlin app
            return StatusCode(500, new { 
                Error = ex.Message, 
                InnerDetail = ex.InnerException?.Message 
            });
        }
    }
}