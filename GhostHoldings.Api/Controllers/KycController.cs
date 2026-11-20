using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Kyc;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class KycController : ControllerBase
{
    private readonly IMediator _mediator;

    public KycController(IMediator mediator) => _mediator = mediator;

    // 🔥 1. USER: Submit KYC
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> SubmitKyc([FromBody] SubmitKycDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        try
        {
            var command = new SubmitKycCommand(userId, request.DocumentType, request.FrontImageBase64, request.BackImageBase64, request.SelfieBase64);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    // 🔥 2. USER: Get KYC Status
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetKycStatus(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { Error = "Invalid token." });

        var result = await _mediator.Send(new GetKycStatusQuery(userId), cancellationToken);
        return Ok(result);
    }

    // 🔥 3. ADMIN: Review KYC (Approve/Reject)
    [HttpPost("admin/review")]
    [Authorize(Roles = "Admin")] // 🔥 Ensure only admins can call this
    public async Task<IActionResult> AdminReviewKyc([FromBody] AdminReviewKycDto request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new AdminReviewKycCommand(request.KycId, request.Status, request.RejectionReason);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

// 🔥 DTOs for the Controller
public record SubmitKycDto(
    string DocumentType, 
    string FrontImageBase64, 
    string? BackImageBase64, 
    string SelfieBase64
);

public record AdminReviewKycDto(
    Guid KycId, 
    string Status, 
    string? RejectionReason
);