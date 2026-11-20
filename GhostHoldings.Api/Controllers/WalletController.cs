using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Wallet;
using GhostHoldings.Application.Interfaces;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] 
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaystackService _paystackService;

    public WalletController(IMediator mediator, IPaystackService paystackService)
    {
        _mediator = mediator;
        _paystackService = paystackService;
    }

    // 🔥 1. GET WALLET BALANCE
    [HttpGet("balance")]
    public async Task<IActionResult> GetWalletBalance(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { Error = "Invalid token." });
        }

        try
        {
            var wallet = await _mediator.Send(new GetWalletQuery(userId), cancellationToken);
            return Ok(wallet);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Detail = ex.InnerException?.Message });
        }
    }

    // 🔥 2. INITIATE DEPOSIT (M-Pesa)
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { Error = "Invalid token." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { Error = "Amount must be greater than 0." });
        }

        try
        {
            // 1. Get user email from DB (or pass it in the request, but fetching from DB is safer)
            // For now, we'll let the MediatR command handle the DB lookup and Paystack initiation
            var command = new InitiateDepositCommand(userId, request.Amount, request.PhoneNumber);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(new { 
                Message = "Deposit initiated. Check your phone for the M-Pesa prompt.",
                Reference = result.Reference,
                AuthorizationUrl = result.AuthorizationUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Detail = ex.InnerException?.Message });
        }
    }

    // 🔥 3. INITIATE WITHDRAWAL (M-Pesa)
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { Error = "Invalid token." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { Error = "Amount must be greater than 0." });
        }

        try
        {
            var command = new InitiateWithdrawalCommand(userId, request.Amount, request.PhoneNumber);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(new { 
                Message = "Withdrawal initiated successfully.",
                Reference = result.Reference,
                Status = result.Status
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Detail = ex.InnerException?.Message });
        }
    }
}

// 🔥 DTOs for the Controller
public record DepositRequestDto(decimal Amount, string PhoneNumber);
public record WithdrawRequestDto(decimal Amount, string PhoneNumber);