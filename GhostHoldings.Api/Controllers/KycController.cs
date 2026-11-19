using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using GhostHoldings.Application.Features.Kyc;

namespace GhostHoldings.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly IMediator _mediator;

    public KycController(IMediator mediator) => _mediator = mediator;

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitKycRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitKycCommand(GetUserId(), request.DocumentType, request.DocumentUrl), cancellationToken);
        return result.Success ? Ok(new { Message = result.Message }) : BadRequest(new { Error = result.Message });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetMyKycStatusQuery(GetUserId()), cancellationToken);
        
        if (status == null)
        {
            return Ok(new { Status = "NotSubmitted", Message = "No KYC submission found." });
        }
        
        return Ok(status);
    }
}

public record SubmitKycRequest(string DocumentType, string DocumentUrl);