using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Kyc;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 🔥 In production: [Authorize(Roles = "Admin")]
public class AdminKycController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminKycController(IMediator mediator) => _mediator = mediator;

    private Guid GetAdminId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var submissions = await _mediator.Send(new GetPendingKycSubmissionsQuery(), cancellationToken);
        return Ok(submissions);
    }

    [HttpPost("{submissionId}/approve")]
    public async Task<IActionResult> Approve(Guid submissionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReviewKycCommand(submissionId, GetAdminId(), true, null), cancellationToken);
        return result.Success ? Ok(new { Message = result.Message }) : BadRequest(new { Error = result.Message });
    }

    [HttpPost("{submissionId}/reject")]
    public async Task<IActionResult> Reject(Guid submissionId, [FromBody] RejectRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReviewKycCommand(submissionId, GetAdminId(), false, request.Reason), cancellationToken);
        return result.Success ? Ok(new { Message = result.Message }) : BadRequest(new { Error = result.Message });
    }
}

public record RejectRequest(string Reason);