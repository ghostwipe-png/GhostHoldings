using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Features.Admin;
using GhostHoldings.Application.Features.Kyc; // <-- CRITICAL FIX
using Microsoft.EntityFrameworkCore;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;

    public AdminController(IMediator mediator, IApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    private async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user?.IsAdmin ?? false;
    }

    [HttpGet("pending-requests")]
    public async Task<IActionResult> GetPendingRequests(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        if (!await IsAdminAsync(userId, cancellationToken))
            return Forbid("Admin access required.");

        var requests = await _mediator.Send(new GetPendingRequestsQuery(), cancellationToken);
        return Ok(requests);
    }

    [HttpPost("process-request")]
    public async Task<IActionResult> ProcessRequest([FromBody] ProcessRequestDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        if (!await IsAdminAsync(userId, cancellationToken))
            return Forbid("Admin access required.");

        try
        {
            await _mediator.Send(new ProcessPendingRequestCommand(dto.RequestId, userId, dto.Action), cancellationToken);
            return Ok(new { Message = $"Request {dto.Action}d successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("refund-transaction")]
    public async Task<IActionResult> RefundTransaction([FromBody] RefundTransactionDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var adminId))
            return Unauthorized(new { Error = "Invalid token." });

        if (!await IsAdminAsync(adminId, cancellationToken))
            return Forbid("Admin access required.");

        try
        {
            await _mediator.Send(new RefundFailedWithdrawalCommand(dto.TransactionId, adminId, dto.Reason), cancellationToken);
            return Ok(new { Message = "Transaction refunded successfully. Funds restored to user wallet.", TransactionId = dto.TransactionId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("pending-kyc")]
    public async Task<IActionResult> GetPendingKyc(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        if (!await IsAdminAsync(userId, cancellationToken))
            return Forbid("Admin access required.");

        var requests = await _mediator.Send(new GetPendingKycSubmissionsQuery(), cancellationToken);
        return Ok(requests);
    }

    [HttpPost("review-kyc")]
    public async Task<IActionResult> ReviewKyc([FromBody] ReviewKycDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var adminId))
            return Unauthorized(new { Error = "Invalid token." });

        if (!await IsAdminAsync(adminId, cancellationToken))
            return Forbid("Admin access required.");

        try
        {
            await _mediator.Send(new ReviewKycSubmissionCommand(dto.SubmissionId, adminId, dto.Action, dto.Reason), cancellationToken);
            return Ok(new { Message = $"KYC submission {dto.Action}d successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public record ProcessRequestDto(Guid RequestId, string Action);
public record RefundTransactionDto(Guid TransactionId, string Reason);
public record ReviewKycDto(Guid SubmissionId, string Action, string? Reason);